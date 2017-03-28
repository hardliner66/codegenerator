unit test;

interface

uses
	System.JSON,
	Generics.Collections,
	m2.Common.Serializer.Interfaces;

type
    IInterfacedData = interface;
    TInterfacedData = class;    TData = class;

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


    end;

    TInterfacedData = class(TInterfacedObject, IIsSerializable, IInterfacedData)
    strict private

    strict private
        type KeyConstants = class abstract
        strict private
            constructor Create(); reintroduce; virtual; abstract; // Prevent anyone from accessing the constructor
        public
            const SomeText1Key = 'some_text';
            const SomeNumber1Key = 'SomeNumber1';
            const SomeObject1Key = 'SomeObject1';
            const SomePrimitiveList1Key = 'SomePrimitiveList1';
            const SomeObjectList1Key = 'SomeObjectList1';

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
            const SomeText1Key = 'some_text';
            const SomeNumber1Key = 'SomeNumber1';
            const SomeObject1Key = 'SomeObject1';
            const SomePrimitiveList1Key = 'SomePrimitiveList1';
            const SomeObjectList1Key = 'SomeObjectList1';

		end;

    public
        SomeText1: String;
        SomeNumber1: Int64;
        SomeObject1: ExternalType;
        SomePrimitiveList1: TList<Integer>;
        SomeObjectList1: TList<ExternalType>;

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

constructor InterfacedData.Create();
begin

end;

destructor InterfacedData.Destroy();
begin

end;


function Serialize(out value: string): Cardinal; overload;
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

function Deserialize(const value: string): Cardinal; overload;
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
end;{Data}

constructor Data.Create();
begin

end;

destructor Data.Destroy();
begin

end;


function Serialize(out value: string): Cardinal; overload;
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

function Deserialize(const value: string): Cardinal; overload;
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