unit test;

interface

uses
	System.JSON,
	Generics.Collections,
	m2.Common.Serializer.Interfaces;

type
    IInterfacedData = interface;
    TInterfacedData = class;
    TData = class;


    IInterfacedData = interface(IIsSerializable)
        function GetSomeText1: String;
        procedure SetSomeText1(const value: String);
        property SomeText1: String read GetSomeText1 write SetSomeText1;


        function GetSomeNumber1: Int64;
        procedure SetSomeNumber1(const value: Int64);
        property SomeNumber1: Int64 read GetSomeNumber1 write SetSomeNumber1;


        function GetSomeObject1: ExternalType;
        procedure SetSomeObject1(const value: ExternalType);
        property SomeObject1: ExternalType read GetSomeObject1 write SetSomeObject1;


        function GetSomePrimitiveList1: TList<Integer>;
        procedure SetSomePrimitiveList1(const value: TList<Integer>);
        property SomePrimitiveList1: TList<Integer> read GetSomePrimitiveList1 write SetSomePrimitiveList1;


        function GetSomeObjectList1: TList<ExternalType>;
        procedure SetSomeObjectList1(const value: TList<ExternalType>);
        property SomeObjectList1: TList<ExternalType> read GetSomeObjectList1 write SetSomeObjectList1;


        function GetSomeCrossReference1: Data;
        procedure SetSomeCrossReference1(const value: Data);
        property SomeCrossReference1: Data read GetSomeCrossReference1 write SetSomeCrossReference1;


    end;

    TInterfacedData = class(TInterfacedObject, IIsSerializable, IInterfacedData)
    strict private

    strict private
        type KeyConstants = class abstract
        strict private
            constructor Create(); reintroduce; virtual; abstract; // Prevent anyone from accessing the constructor
        public
            const SomeText1Key = 'some_text';
            const SomeObject1Key = 'SomeObject1';
            const SomePrimitiveList1Key = 'SomePrimitiveList1';
            const SomeObjectList1Key = 'SomeObjectList1';
            const SomeCrossReference1Key = 'SomeCrossReference1';

        end;


    strict private

    public
        function GetSomeText1: String;
        procedure SetSomeText1(const value: String);
        property SomeText1: String read GetSomeText1 write SetSomeText1;

        function GetSomeNumber1: Int64;
        procedure SetSomeNumber1(const value: Int64);
        property SomeNumber1: Int64 read GetSomeNumber1 write SetSomeNumber1;

        function GetSomeObject1: ExternalType;
        procedure SetSomeObject1(const value: ExternalType);
        property SomeObject1: ExternalType read GetSomeObject1 write SetSomeObject1;

        function GetSomePrimitiveList1: TList<Integer>;
        procedure SetSomePrimitiveList1(const value: TList<Integer>);
        property SomePrimitiveList1: TList<Integer> read GetSomePrimitiveList1 write SetSomePrimitiveList1;

        function GetSomeObjectList1: TList<ExternalType>;
        procedure SetSomeObjectList1(const value: TList<ExternalType>);
        property SomeObjectList1: TList<ExternalType> read GetSomeObjectList1 write SetSomeObjectList1;

        function GetSomeCrossReference1: Data;
        procedure SetSomeCrossReference1(const value: Data);
        property SomeCrossReference1: Data read GetSomeCrossReference1 write SetSomeCrossReference1;


        constructor Create();
        destructor Destroy(); override;

        function Serialize(out value: string): Cardinal; overload;
        function Deserialize(const value: string): Cardinal; overload;

        function Serialize(out value: TJSONValue): Cardinal; overload;
        function Deserialize(const value: TJSONValue): Cardinal; overload;

    end;

    TData = class(TInterfacedObject, IIsSerializable)
    strict private

    strict private
        type KeyConstants = class abstract
        strict private
			constructor Create(); reintroduce; virtual; abstract; // Prevent anyone from accessing the constructor
		public
            const SomeText2Key = 'some_text';
            const SomeObject2Key = 'SomeObject2';
            const SomePrimitiveList2Key = 'SomePrimitiveList2';
            const SomeObjectList2Key = 'SomeObjectList2';
            const SomeCrossReference2Key = 'SomeCrossReference2';

		end;

    public
        SomeText2: String;
        SomeNumber2: Int64;
        SomeObject2: ExternalType;
        SomePrimitiveList2: TList<Integer>;
        SomeObjectList2: TList<ExternalType>;
        SomeCrossReference2: InterfacedData;

        constructor Create();
        destructor Destroy(); override;

        function Serialize(out value: string): Cardinal; overload;
        function Deserialize(const value: string): Cardinal; overload;

        function Serialize(out value: TJSONValue): Cardinal; overload;
        function Deserialize(const value: TJSONValue): Cardinal; overload;

    end;



implementation

uses
	System.SysUtils,
	m2.Common.JsonHelper,
	m2.Common.Memory;

{InterfacedData}

constructor TInterfacedData.Create();
begin
	_SomeObject1 := ExternalType.Create();
	_SomePrimitiveList1 := TList<Integer>.Create();
	_SomeObjectList1 := TList<ExternalType>.Create();
	_SomeCrossReference1 := Data.Create();
end;

destructor TInterfacedData.Destroy();
begin
	TCommonMemory.SafeFreeAndNil(_SomePrimitiveList1);
	TCommonMemory.SafeFreeAndNil(_SomeObjectList1);
	TCommonMemory.SafeFreeAndNil(_SomeCrossReference1);
end;

function InterfacedData.GetSomeText1: String;
begin
	Result := _SomeText1;
end;

procedure InterfacedData.SetSomeText1(const value: String);
begin
	_SomeText1 := value;
end;

function InterfacedData.GetSomeNumber1: Int64;
begin
	Result := _SomeNumber1;
end;

procedure InterfacedData.SetSomeNumber1(const value: Int64);
begin
	_SomeNumber1 := value;
end;

function InterfacedData.GetSomeObject1: ExternalType;
begin
	Result := _SomeObject1;
end;

procedure InterfacedData.SetSomeObject1(const value: ExternalType);
begin
	_SomeObject1 := value;
end;

function InterfacedData.GetSomePrimitiveList1: Integer;
begin
	Result := _SomePrimitiveList1;
end;

procedure InterfacedData.SetSomePrimitiveList1(const value: Integer);
begin
	_SomePrimitiveList1 := value;
end;

function InterfacedData.GetSomeObjectList1: ExternalType;
begin
	Result := _SomeObjectList1;
end;

procedure InterfacedData.SetSomeObjectList1(const value: ExternalType);
begin
	_SomeObjectList1 := value;
end;

function InterfacedData.GetSomeCrossReference1: Data;
begin
	Result := _SomeCrossReference1;
end;

procedure InterfacedData.SetSomeCrossReference1(const value: Data);
begin
	_SomeCrossReference1 := value;
end;


function TInterfacedData.Serialize(out value: string): Cardinal; overload;
var
	jsonObject: TJSONValue;
begin
	value := '';
	try
		Result := Serialize(jsonObject);
		value := jsonObject.ToJSON();
	finally
		TCommonMemory.SafeFreeAndNil(jsonObject);
	end;
end;

function TInterfacedData.Deserialize(const value: string): Cardinal; overload;
var
	jsonValue: TJSONValue;
begin
	if value.Trim = '' then
	begin
		if EMPTY then
		begin
			exit(SERIALIZATION_SUCCEEDED);
		end
		else
		begin
			exit(SERIALIZATION_FAILED);
		end;
	end;

	try
		jsonValue := TJSONObject.ParseJSONValue(value);
		exit(Deserialize(jsonValue));
	finally
		TCommonMemory.SafeFreeAndNil(jsonValue);
	end;
end;



{Data}

constructor TData.Create();
begin
	SomeObject2 := ExternalType.Create();
	SomePrimitiveList2 := TList<Integer>.Create();
	SomeObjectList2 := TList<ExternalType>.Create();
	SomeCrossReference2 := InterfacedData.Create();
end;

destructor TData.Destroy();
begin
	TCommonMemory.SafeFreeAndNil(SomePrimitiveList2);
	TCommonMemory.SafeFreeAndNil(SomeObjectList2);
end;


function TData.Serialize(out value: string): Cardinal; overload;
var
	jsonObject: TJSONValue;
begin
	value := '';
	try
		Result := Serialize(jsonObject);
		value := jsonObject.ToJSON();
	finally
		TCommonMemory.SafeFreeAndNil(jsonObject);
	end;
end;

function TData.Deserialize(const value: string): Cardinal; overload;
var
	jsonValue: TJSONValue;
begin
	if value.Trim = '' then
	begin
		if EMPTY then
		begin
			exit(SERIALIZATION_SUCCEEDED);
		end
		else
		begin
			exit(SERIALIZATION_FAILED);
		end;
	end;

	try
		jsonValue := TJSONObject.ParseJSONValue(value);
		exit(Deserialize(jsonValue));
	finally
		TCommonMemory.SafeFreeAndNil(jsonValue);
	end;
end;




end.