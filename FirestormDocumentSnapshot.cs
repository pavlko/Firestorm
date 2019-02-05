using System;
using UnityEngine;
using System.Linq;
using E7.Firebase.LitJson;

using ValueObject = System.Collections.Generic.Dictionary<string, object>;
using System.Collections.Generic;

namespace E7.Firebase
{
    public class ArrayData
    {
        public ValueObject[] values;
    }

    [Serializable]
    public struct FirestormDocumentSnapshot
    {
        private FirestormDocument document;
        private string formattedDataJson;

        public string Name => document.name;
        public FirestormDocument Document => document;

        // public string name;
        // public DateTime createTime;
        // public DateTime updateTime;
        // public JToken fields;
        //public IEnumerable<JProperty> properties;
        public bool IsEmpty { private set; get; }


        public static FirestormDocumentSnapshot Empty
        {
            get
            {
                return new FirestormDocumentSnapshot { IsEmpty = true };
            }
        }

        public T ConvertTo<T>() where T : class
        {
            if (IsEmpty)
            {
                throw new FirestormException($"The document snapshot is empty, please check for IsEmpty instead of trying to convert into an empty instance.");
            }

            //Leave it to LitJSON, we have formatted the json to be ready for convert.
            return JsonMapper.ToObject<T>(formattedDataJson);
        }

        /// <summary>
        /// DocumentMask : https://firebase.google.com/docs/firestore/reference/rest/v1beta1/DocumentMask
        /// </summary>
        internal string FieldsDocumentMaskJson()
        {
            if (IsEmpty)
            {
                return JsonMapper.ToJson(new DocumentMask());
            }
            else
            {
                //handle nested fields!!
                var topLevelFields = this.document.fields.Where(x => x.Value.Keys.First() != "mapValue").Select(x => x.Key);
                var mapFields = this.document.fields.Where(x => x.Value.Keys.First() == "mapValue")
                .SelectMany(x =>
                {
                    string parentFieldName = x.Key;
                    var mapFieldNames = ((JsonData)((ValueObject)x.Value["mapValue"])["fields"]).Keys;
                    return mapFieldNames.Select(y => $"{parentFieldName}.{y}");
                });

                var mask = new DocumentMask { fieldPaths = topLevelFields.Concat(mapFields).ToArray() };
                var m = JsonMapper.ToJson(mask);
                Debug.Log($"Made mask {m}");
                return m;
            }
        }

        public FirestormDocumentSnapshot(string jsonString)
        {
            Debug.Log($"Snapshot from {jsonString}");
            //File.WriteAllText(Application.dataPath + $"/{UnityEngine.Random.Range(0, 100)}.txt", jsonString);
            IsEmpty = false;

            this.document = JsonMapper.ToObject<FirestormDocument>(jsonString);

            //Write in a format that can be map to any object by LitJSON
            var writer = new JsonWriter();
            writer.PrettyPrint = true;
            writer.WriteObjectStart();
            foreach (var field in document.fields.Keys)
            {
                var insideValueText = document.fields[field].First().Key;
                var insideValue = document.fields[field].First().Value;
                writer.WritePropertyName(field);
                ValueTextToWrite(insideValueText, insideValue);
            }
            writer.WriteObjectEnd();

            formattedDataJson =  writer.ToString();
            Debug.Log($"{formattedDataJson}");

            void ValueTextToWrite(string valueText, object value)
            {
                if(value is JsonData jd)
                {
                    value = (JsonData)jd;
                }

                switch (valueText)
                {
                    case "integerValue":
                        //Integer is dangerous because it came as string of number
                        writer.Write(int.Parse((string)value));
                        break;
                    case "doubleValue":
                        writer.Write((double)value);
                        break;
                    case "booleanValue":
                        writer.Write((bool)value);
                        break;
                    case "arrayValue":
                        writer.WriteArrayStart();
                        JsonData al = (JsonData)((Dictionary<string, object>)value)["values"];
                        foreach(JsonData a in al)
                        {
                            //If you put array in array it may explode here
                            ValueTextToWrite(a.Keys.First(), a[a.Keys.First()].UnderlyingPrimitive());
                        }
                        writer.WriteArrayEnd();
                        break;
                    case "mapValue":
                        writer.WriteObjectStart();
                        JsonData alObj = (JsonData)((Dictionary<string, object>)value)["fields"];
                        //Debug.Log($"{alObj} {alObj.Count} {alObj.IsArray} {alObj.IsObject}");
                        foreach (KeyValuePair<string, JsonData> a in alObj)
                        {
                            writer.WritePropertyName(a.Key);
                            JsonData mapFieldData = a.Value;
                            var firstKey = mapFieldData.Keys.First();
                            //Debug.Log($"Working on {a.Key} {firstKey} {mapFieldData[firstKey].UnderlyingPrimitive()} object map");
                            ValueTextToWrite(firstKey, mapFieldData[firstKey].UnderlyingPrimitive());
                        }
                        writer.WriteObjectEnd();
                        break;
                    default:
                        //Debug.Log($"AHA {valueText} {value} {value?.GetType().Name}");
                        string casted = (string)value;
                        writer.Write(casted);
                        //Debug.Log($"AHAhh {valueText} {value}");
                        break;
                }
            }

            //var jo = JObject.Parse(jsonString);

            // if (jo.ContainsKey(nameof(name)) &&
            //     jo.ContainsKey(nameof(fields)))
            // {
            //     name = (string)jo[nameof(name)];
            //     fields = jo[nameof(fields)];
            //     properties = fields.Children<JProperty>();
            //     if (jo.ContainsKey(nameof(createTime)) &&
            //         jo.ContainsKey(nameof(updateTime)))
            //     {
            //         createTime = jo[nameof(createTime)].ToObject<DateTime>();
            //         updateTime = jo[nameof(updateTime)].ToObject<DateTime>();
            //     }
            //     else
            //     {
            //         createTime = default;
            //         updateTime = default;
            //     }
            // }
            // else
            // {
            //     throw new FirestormException($"This object is not a document! {jsonString}");
            // }
        }

        public override string ToString() => $"{document.name} : {document.createTime} {document.updateTime} Fields {document.fields.ToString()}";
    }

}