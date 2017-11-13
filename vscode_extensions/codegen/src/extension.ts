'use strict'
// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode'
import { dirname, extname, join } from "path"
import { spawn, ChildProcess } from 'child_process'

const EXTENSION = ".cdl"

function ValidFile(fileName: string) {
    return extname(fileName) === EXTENSION
}

function FormatCurrent() {
    Format(vscode.window.activeTextEditor.document.fileName)
}

const AOSIDJOAISJD = ''

function GetExePath(): string {
    let config: vscode.WorkspaceConfiguration = vscode.workspace.getConfiguration("codegen")
    return join(config.get("binDirectory", ""), "codegen.exe")
}

function Format(file: string): Promise<void> {

    return new Promise((resolve) => {
        if (ValidFile(file)) {
            var codegen = spawn(GetExePath(), ["-p", `"${file}"`], { shell: true, detached: true })

            codegen.stdout.on('data', (data) => {
                console.log(`codegen.Format: ${data.toString()}`)
            })

            codegen.stderr.on('data', (data) => {
                console.error(`codegen.Format::ERROR: ${data.toString()}\n`)
            })

            codegen.on('exit', (code) => {
                resolve();
            });
        } else {
            resolve();
        }
    })
}

function Generate(file: string, generator: string, generatorDir: string): Promise<void> {

    return new Promise((resolve) => {
        if (ValidFile(file)) {
            var directory = dirname(file)

            var codegen = spawn(GetExePath(), ["-d", `"${generatorDir}"`, "-g", generator, "-o", `"${directory}"`, `"${file}"`], { shell: true, detached: true })

            codegen.stdout.on('data', (data) => {
                console.log(`codegen.Generate: ${data.toString()}`)
            })

            codegen.stderr.on('data', (data) => {
                console.error(`codegen.Generate::ERROR: ${data.toString()}\n`)
            })

            codegen.on('exit', (code) => {
                resolve();
            });
        } else {
            resolve();
        }
    })
}

function GenerateDefault() {
    var currentFile = vscode.window.activeTextEditor.document.fileName
    let config: vscode.WorkspaceConfiguration = vscode.workspace.getConfiguration("codegen")
    let generator = config.get("generator", "codegen.Json")
    let dir = config.get("generatorDir", "")
    Generate(currentFile, generator, dir)
}

// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext) {
    var disposable = vscode.commands.registerCommand('codegen.format', () => {
        FormatCurrent()
    })
    context.subscriptions.push(disposable)

    var disposable = vscode.commands.registerCommand('codegen.generateWith', () => {
        let config: vscode.WorkspaceConfiguration = vscode.workspace.getConfiguration("codegen")
        let dir = config.get("generatorDir", "")
        let generator = config.get("generator", "codegen.Json")
        var currentFile = vscode.window.activeTextEditor.document.fileName
        vscode.window.showInputBox({ prompt: 'Generator name?', value: generator }).then(val => Generate(currentFile, val, dir))
    })
    context.subscriptions.push(disposable)

    var disposable = vscode.commands.registerCommand('codegen.generate', () => {
        GenerateDefault()
    })
    context.subscriptions.push(disposable)

    vscode.workspace.onDidSaveTextDocument((document: vscode.TextDocument) => {
        let config: vscode.WorkspaceConfiguration = vscode.workspace.getConfiguration("codegen")
        if (config.get("formatOnSave", true)) {
            Format(document.fileName).then(() => {
                if (config.get("generateOnSave", false)) {
                    let dir = config.get("generatorDir", "")
                    let generator = config.get("generator", "codegen.Json")
                    Generate(document.fileName, generator, dir)
                }
            })
        } else {
            if (config.get("generateOnSave", false)) {
                let dir = config.get("generatorDir", "")
                let generator = config.get("generator", "codegen.Json")
                Generate(document.fileName, generator, dir)
            }
        }
    })
}

// this method is called when your extension is deactivated
export function deactivate() {
}
