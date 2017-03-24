namespace Codegen {
    public class Extensions {
        public string ToJavaType(string type) {
            string result = type;
            switch (type.ToLower())
            {
                case "string":
                    result = "String";
                    break;
                default:
                    break;
            }
            return result;
        }
    }

    public class Template<T>: CustomizedTemplate<T> {
        public Template() : base() {
            Extensions = new Extensions();
        }

        public Extensions Extensions {get;set;}
    }
}