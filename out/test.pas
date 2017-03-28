unit test;

interface

uses
	System.JSON,
	Generics.Collections,
	m2.Common.Serializer.Interfaces;

type
    IDisabledRisError = interface;
    TDisabledRisError = class;    IDisabledRisErrorList = interface;
    TDisabledRisErrorList = class;    IGetRisConfigurationResponseList = interface;
    TGetRisConfigurationResponseList = class;    ISomeObject = interface;
    TSomeObject = class;

    IDisabledRisError = interface(IIsSerializable)
        function GetErrorcode: String;
        procedure SetErrorcode(const value: String);
        property Errorcode: String read GetErrorcode write SetErrorcode;


        function GetDisabledUntil: int64;
        procedure SetDisabledUntil(const value: int64);
        property DisabledUntil: int64 read GetDisabledUntil write SetDisabledUntil;


    end;

    TDisabledRisError = class(TInterfacedObject, IIsSerializable, IDisabledRisError)
    strict private

    strict private
        type KeyConstants = class abstract
        strict private
            constructor Create(); reintroduce; virtual; abstract; // Prevent anyone from accessing the constructor
        public
            const ErrorcodeKey = 'error_code';
            const DisabledUntilKey = 'disabled_until';

        end;


    strict private

    public
        function GetErrorcode: String;
        procedure SetErrorcode(const value: String);
        property Errorcode: String read GetErrorcode write SetErrorcode;

        function GetDisabledUntil: int64;
        procedure SetDisabledUntil(const value: int64);
        property DisabledUntil: int64 read GetDisabledUntil write SetDisabledUntil;


        function Serialize(out value: string): Cardinal; overload;
        function Deserialize(const value: string): Cardinal; overload;

        function Serialize(out value: TJSONValue): Cardinal; overload;
        function Deserialize(const value: TJSONValue): Cardinal; overload;

    end;

    IDisabledRisErrorList = interface(IIsSerializable)
        function GetErrors: TList<DisabledRisError>;
        procedure SetErrors(const value: TList<DisabledRisError>);
        property Errors: TList<DisabledRisError> read GetErrors write SetErrors;


    end;

    TDisabledRisErrorList = class(TInterfacedObject, IIsSerializable, IDisabledRisErrorList)
    strict private

    strict private
        type KeyConstants = class abstract
        strict private
            constructor Create(); reintroduce; virtual; abstract; // Prevent anyone from accessing the constructor
        public
            const ErrorsKey = 'errors';

        end;


    strict private

    public
        function GetErrors: TList<DisabledRisError>;
        procedure SetErrors(const value: TList<DisabledRisError>);
        property Errors: TList<DisabledRisError> read GetErrors write SetErrors;


        constructor Create();
        destructor Destroy(); override;
        function Serialize(out value: string): Cardinal; overload;
        function Deserialize(const value: string): Cardinal; overload;

        function Serialize(out value: TJSONValue): Cardinal; overload;
        function Deserialize(const value: TJSONValue): Cardinal; overload;

    end;

    IGetRisConfigurationResponseList = interface(IIsSerializable)
        function GetNONE: NONE;
        procedure SetNONE(const value: NONE);
        property NONE: NONE read GetNONE write SetNONE;


    end;

    TGetRisConfigurationResponseList = class(TInterfacedObject, IIsSerializable, IGetRisConfigurationResponseList)
    strict private

    strict private
        type KeyConstants = class abstract
        strict private
            constructor Create(); reintroduce; virtual; abstract; // Prevent anyone from accessing the constructor
        public
            const NONEKey = 'NONE';

        end;


    strict private

    public
        function GetNONE: NONE;
        procedure SetNONE(const value: NONE);
        property NONE: NONE read GetNONE write SetNONE;


        constructor Create();
        destructor Destroy(); override;
        function Serialize(out value: string): Cardinal; overload;
        function Deserialize(const value: string): Cardinal; overload;

        function Serialize(out value: TJSONValue): Cardinal; overload;
        function Deserialize(const value: TJSONValue): Cardinal; overload;

    end;

    ISomeObject = interface(IIsSerializable)
        function GetSomeValue: String;
        procedure SetSomeValue(const value: String);
        property SomeValue: String read GetSomeValue write SetSomeValue;


        function GetSomeOtherValue: TList<Boolean>;
        procedure SetSomeOtherValue(const value: TList<Boolean>);
        property SomeOtherValue: TList<Boolean> read GetSomeOtherValue write SetSomeOtherValue;


        function GetSomeQuotedValue: TList<String>;
        procedure SetSomeQuotedValue(const value: TList<String>);
        property SomeQuotedValue: TList<String> read GetSomeQuotedValue write SetSomeQuotedValue;


    end;

    TSomeObject = class(TInterfacedObject, IIsSerializable, ISomeObject)
    strict private

    strict private
        type KeyConstants = class abstract
        strict private
            constructor Create(); reintroduce; virtual; abstract; // Prevent anyone from accessing the constructor
        public
            const SomeValueKey = 'some_value';
            const SomeOtherValueKey = 'some_other_value';
            const SomeQuotedValueKey = 'some other value';

        end;


    strict private

    public
        function GetSomeValue: String;
        procedure SetSomeValue(const value: String);
        property SomeValue: String read GetSomeValue write SetSomeValue;

        function GetSomeOtherValue: TList<Boolean>;
        procedure SetSomeOtherValue(const value: TList<Boolean>);
        property SomeOtherValue: TList<Boolean> read GetSomeOtherValue write SetSomeOtherValue;

        function GetSomeQuotedValue: TList<String>;
        procedure SetSomeQuotedValue(const value: TList<String>);
        property SomeQuotedValue: TList<String> read GetSomeQuotedValue write SetSomeQuotedValue;


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

{DisabledRisError}



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
end;{DisabledRisErrorList}

constructor DisabledRisErrorList.Create();
begin

end;

destructor DisabledRisErrorList.Destroy();
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
end;{GetRisConfigurationResponseList}

constructor GetRisConfigurationResponseList.Create();
begin

end;

destructor GetRisConfigurationResponseList.Destroy();
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
end;{SomeObject}

constructor SomeObject.Create();
begin

end;

destructor SomeObject.Destroy();
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