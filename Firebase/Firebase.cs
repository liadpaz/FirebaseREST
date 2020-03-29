﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using static Firebase.Firestore.Document;

namespace Firebase {
#pragma warning disable IDE0044, IDE0051 , IDE0052, 169, 414, 649
    #region Firebase

    #region General Firebase

    /// <summary>
    /// This class is for the general Firebase
    /// </summary>
    public sealed class Firebase {
        private static Firebase firebase = null;
        private static FirebaseAuth auth = null;
        private static FirebaseDatabase database = null;
        private static Firestore firestore = null;

        internal static string ProjectId {
            private set;
            get;
        }
        internal static string ApiKey {
            private set;
            get;
        } = null;

        /// <summary>
        /// This function initializes the only Firebase instance on the app
        /// </summary>
        /// <param name="projectId">The Firebase project name</param>
        /// <param name="apiKey">The Web Api for the Firebase project</param>
        /// <returns>The only Firebase instance on the app</returns>
        public static Firebase InitializeFirebase(string projectId, string apiKey) {
            if (firebase == null) {
                ProjectId = projectId;
                ApiKey = apiKey;
                auth = new FirebaseAuth(apiKey);
                database = new FirebaseDatabase();
                firestore = new Firestore();
                firebase = new Firebase();
            }
            return firebase;
        }

        /// <summary>
        /// This function returns the only Firebase instance on the app
        /// </summary>
        /// <returns>the only Firebase instance on the app</returns>
        public static Firebase GetInstance() => firebase;

        /// <summary>
        /// Private constructor so you won't be able to create new instance from outside the class
        /// </summary>
        private Firebase() { }

        /// <summary>
        /// This getter returns the only instance of the Firebase Real-Time Database
        /// </summary>
        public FirebaseDatabase FirebaseDatabase => database;

        /// <summary>
        /// This getter returns the only instance of the Firebase Authentication
        /// </summary>
        public FirebaseAuth FirebaseAuth => auth;

        /// <summary>
        /// This getter returns the only instance of the Firebase Firestore
        /// </summary>
        public Firestore FirebaseFirestore => firestore;
    }

    #endregion General Firebase

    #region Real-Time Database

    /// <summary>
    /// This class is for the Firebase Real-Time Database
    /// </summary>
    public sealed class FirebaseDatabase {
        /// <summary>
        /// This function returns the reference to the Firebase Database at the <code>child</code> child
        /// </summary>
        /// <param name="child">the child</param>
        /// <returns>the reference to the Firebase Database at the <code>child</code> child</returns>
        public DatabaseReference GetReference(string child = null) => new DatabaseReference($"{child}");
    }

    /// <summary>
    /// This class is for the Firebase Real-Time Database reference
    /// </summary>
    public sealed class DatabaseReference {
        private static readonly HttpClient client = new HttpClient();

        private readonly string child;

        /// <summary>
        /// Internal constructor that can only be called from within this library, sets the child of the database reference
        /// </summary>
        /// <param name="child">the database URL</param>
        internal DatabaseReference(string child = null) {
            this.child = child;
            if (client.BaseAddress == null) {
                client.BaseAddress = new Uri($"https://{Firebase.ProjectId}.firebaseio.com/");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
        }

        /// <summary>
        /// Private constructor so the user won't be able to create a DatabaseReference
        /// </summary>
        private DatabaseReference() { }

        /// <summary>
        /// This function returns the data from the database reference
        /// </summary>
        /// <returns>the data in the database</returns
        public async Task<string> Read() {
            string auth = (Firebase.GetInstance().FirebaseAuth.GetCurrentUser().idToken ?? null)?.Insert(0, "&auth=");
            HttpResponseMessage response = await client.GetAsync($"{child}.json?print=pretty{auth}");

            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
        }

        /// <summary>
        /// This function writes data in the database reference; return true if succeeded otherwise false
        /// </summary>
        /// <param name="data">the data to write</param>
        /// <returns>true if succeeded otherwise false</returns>
        public async Task<bool> Write(string data) {
            string auth = (Firebase.GetInstance().FirebaseAuth.GetCurrentUser().idToken ?? null)?.Insert(0, "?auth=");
            HttpResponseMessage response = await client.PutAsync($"{child}.json{auth}", new StringContent(data));

            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// This function deletes the data in the child in the database
        /// </summary>
        /// <returns>true if succeeded otherwise false</returns>
        public async Task<bool> Delete() {
            string auth = (Firebase.GetInstance().FirebaseAuth.GetCurrentUser().idToken ?? null)?.Insert(0, "?auth=");
            HttpResponseMessage response = await client.DeleteAsync($"{child}.json{auth}");

            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// This function returns a database reference to the <c>child</c> of the current reference
        /// </summary>
        /// <param name="child">the child</param>
        /// <returns>database reference to <c>child</c> of the current reference</returns>
        public DatabaseReference Child(string child) => new DatabaseReference($"{this.child}/{child}");

        /// <summary>
        /// This function returns the reference to the root of the database
        /// </summary>
        /// <returns>the reference to the root of the database</returns>
        public DatabaseReference Root => new DatabaseReference();

        /// <summary>
        /// This function returns the reference to the parent of the current reference
        /// </summary>
        /// <returns>the reference to the parent of the current reference</returns>
        public DatabaseReference GetParent() => new DatabaseReference(child.Substring(0, child.LastIndexOf('/')));

        /// <summary>
        /// This fuction returns the database reference as string, it shows the path of the reference
        /// </summary>
        /// <returns>the database reference as string</returns>
        public override string ToString() => $"{client.BaseAddress}{child}";
    }

    #endregion Real-Time Database

    #region Firestore

    /// <summary>
    /// This class if for the Firestore database
    /// </summary>
    public sealed class Firestore {

        internal readonly static Regex documentRegex = new Regex("^[^/]+([/][^/]*)(([/][^/]*){2})*[^/]$");
        internal readonly static Regex collectionRegex = new Regex("^[^/]+(([/][^/]+){2})*$");

        internal static readonly HttpClient client = new HttpClient();

        internal Firestore() {
            if (client.BaseAddress == null) {
                client.BaseAddress = new Uri($"https://firestore.googleapis.com/");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
        }

        /// <summary>
        /// This function returns a reference to a collection, and it checks if the path is valid
        /// </summary>
        /// <param name="path">The path to the collection</param>
        /// <exception cref="ArgumentException">If the path is invalid</exception>
        /// <returns>The reference to the collection in the path</returns>
        public CollectionReference GetCollectionReference(string path = null) {
            if (string.IsNullOrEmpty(path)) {
                return new CollectionReference();
            }
            if (collectionRegex.IsMatch(path)) {
                return new CollectionReference(path);
            }
            throw new ArgumentException("Invalid path to collection", nameof(path));
        }

        /// <summary>
        /// This function returns a reference to a document, and it checks if the path is valid
        /// </summary>
        /// <param name="path">The path to the document</param>
        /// <exception cref="ArgumentException">If the path is invalid</exception>
        /// <returns>The reference to the document in the path</returns>
        public DocumentReference GetDocumentReference(string path = null) {
            if (documentRegex.IsMatch(path)) {
                return new DocumentReference(path);
            }
            throw new ArgumentException("Invalid path to document", nameof(path));
        }

        /// <summary>
        /// This function creates a query string from the queries entries
        /// </summary>
        /// <param name="queries">The queries to add to the string</param>
        /// <returns>The query string</returns>
        private static string CreateQueryString(params (string key, object value)[] queries) {
            IEnumerable<string> query = from value in queries
                                        where value.value != null
                                        select $"{value.key}={value.value}";
            return query.ToArray().Length != 0 ? $"?{string.Join("&", query)}" : null;
        }

        /// <summary>
        /// This function trims the path
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>The trimmed path</returns>
        /// <example>Input: path/to/document => Output: /path/to/document</example>
        private static string TrimPath(string path = null) => (path ?? null)?.Insert(0, "/");

        /// <summary>
        /// This function trims the last path piece
        /// </summary>
        /// <param name="path">The path to trim</param>
        /// <returns>The path w/o it's last piece</returns>
        private static string TrimLastPathPiece(string path) => path.Substring(0, path.LastIndexOf("/"));

        /// <summary>
        /// This class is for Firestore collection reference
        /// </summary>
        public sealed class CollectionReference {

            public string Path { get; }

            internal CollectionReference(string path = null) => Path = path;

            private CollectionReference() { }

            /// <summary>
            /// This function returns the Id of the current collection
            /// </summary>
            /// <returns>the Id of the current collection</returns>
            public string GetId() => Path.Substring(Path.LastIndexOf("/") + 1);

            /// <summary>
            /// This function creates a document in the Firestore database
            /// </summary>
            /// <param name="documentName">name of the document, can be null</param>
            /// <param name="document">the document to create</param>
            /// <returns>the document created</returns>
            public async Task<Document> CreateDocument(string documentName, Document document) {
                HttpResponseMessage response = await client.PostAsync($"v1/projects/{Firebase.ProjectId}/databases/(default)/documents{TrimPath(Path)}/{(documentName ?? null)?.Insert(0, "?documentId=")}", new StringContent(document.ToString()));

                return JsonConvert.DeserializeObject<Document>(await response.Content.ReadAsStringAsync());
            }

            /// <summary>
            /// This function returns the reference to the document in the specified path
            /// </summary>
            /// <param name="path">the path to the document</param>
            /// <exception cref="ArgumentException">if the path doesn't match a path to a document</exception>
            /// <returns>the reference to the document in the specified path</returns>
            public DocumentReference GetDocumentReference(string path) {
                if (documentRegex.IsMatch($"{Path}/{path}")) {
                    return new DocumentReference($"{Path}/{path}");
                }
                throw new ArgumentException("Invalid path to document", nameof(path));
            }

            /// <summary>
            /// This function returns the <code>DocumentReference</code> parent of the current document reference
            /// </summary>
            /// <returns>the <code>DocumentReference</code> parent of the current document reference</returns>
            public DocumentReference GetParent() => new DocumentReference(TrimLastPathPiece(Path));

            /// <summary>
            /// This function gets the list of the documents in the current path
            /// </summary>
            /// <param name="collectionId">The collection, can be null</param>
            /// <param name="pageSize">Max pages to get, can be null</param>
            /// <param name="pageToken">The last page token retrieved from the last fetch, can be null</param>
            /// <param name="orderBy">The order parameter</param>
            /// <returns>List of the documents in the path</returns>
            public async Task<DocumentList> ListDocuments(string collectionId = null, int? pageSize = null, string pageToken = null, string orderBy = null) {
                string query = CreateQueryString(("pageSize", pageSize), ("pageToken", pageToken), ("orderBy", orderBy));
                HttpResponseMessage response = await client.GetAsync($"v1/projects/{Firebase.ProjectId}/databases/(default)/documents{TrimPath(Path)}/{collectionId}{query}");

                return JsonConvert.DeserializeObject<DocumentList>(await response.Content.ReadAsStringAsync());
            }
        }

        /// <summary>
        /// This class is for Firestore document reference
        /// </summary>
        public sealed class DocumentReference {

            public string Path { get; }

            internal DocumentReference(string path = null) => Path = path;

            private DocumentReference() { }

            /// <summary>
            /// This function returns the Id of the current document
            /// </summary>
            /// <returns>The Id of the current document</returns>
            public string GetId() => Path.Substring(Path.LastIndexOf("/") + 1);

            /// <summary>
            /// This function returns the reference to the sub collection in the path specified
            /// </summary>
            /// <param name="path">the path of the collection</param>
            /// <exception cref="ArgumentException">if the path doesn't match a path to a collection</exception>
            /// <returns>the reference to the collection in the path</returns>
            public CollectionReference Collection(string path) {
                if (collectionRegex.IsMatch($"{Path}/{path}")) {
                    return new CollectionReference($"{Path}/{path}");
                }
                throw new ArgumentException("Invalid path to collection", nameof(path));
            }

            /// <summary>
            /// This function returns an instance of a transaction builder
            /// </summary>
            /// <param name="mode">The mode of the transaction, readonly or readwrite</param>
            /// <param name="param">The mode parameter</param>
            /// <returns>An instance of a transaction builder</returns>
            public TransactionBuilder TransactionBuilder(TransactionBuilder.TransactionOptions.Mode mode, object param) => new TransactionBuilder(Path, mode, param);

            /// <summary>
            /// This function deletes the current document from Firestore database
            /// </summary>
            /// <returns><see langword="true"/> if the document deleted, otherwise <see langword="false"/></returns>
            public async Task<bool> DeleteDocument() {
                HttpResponseMessage response = await client.DeleteAsync($"v1/projects/{Firebase.ProjectId}/databases/(default)/documents{TrimPath(Path)}");

                return response.IsSuccessStatusCode;
            }

            /// <summary>
            /// This function updates the current document from the database
            /// </summary>
            /// <returns><see langword="true"/> if succeeded, otherwise <see langword="false"/></returns>
            public async Task<Document> Get() {
                HttpResponseMessage response = await client.GetAsync($"v1/projects/{Firebase.ProjectId}/databases/(default)/documents{TrimPath(Path)}");

                return JsonConvert.DeserializeObject<Document>(await response.Content.ReadAsStringAsync());
            }

            /// <summary>
            /// This function returns the <code>CollectionReference</code> parent of the current document reference
            /// </summary>
            /// <returns>The CollectionReference parent of the current document reference</returns>
            public CollectionReference GetParent() => new CollectionReference(TrimLastPathPiece(Path));

            /// <summary>
            /// This function sets the content of the current document to the specified document, it overrides any content that pre-existed
            /// </summary>
            /// <param name="document">The document to set to</param>
            /// <returns>The content of the document</returns>
            public async Task<Document> Set(Document document) => (await DeleteDocument()) ? await Update(document) : null;

            /// <summary>
            /// This function update the content of the current document to the specified document
            /// </summary>
            /// <param name="document">The document to update to</param>
            /// <param name="currentDocument">Condition of the current state of the document</param>
            /// <returns>The updated document</returns>
            public async Task<Document> Update(Document document, Precondition currentDocument = null) {
                string query = CreateQueryString(currentDocument != null ? currentDocument.GetQuery() : (null, null));
                HttpResponseMessage response = await client.PatchAsync($"v1/projects/{Firebase.ProjectId}/databases/(default)/documents{TrimPath(Path)}{query}", new StringContent(document.ToString()));

                return JsonConvert.DeserializeObject<Document>(await response.Content.ReadAsStringAsync());
            }

            /// <summary>
            /// This function gets the list of collections in the current document
            /// </summary>
            /// <param name="pageSize">The max number of collections to show</param>
            /// <param name="pageToken">The page token aqquired from this function response</param>
            /// <returns>The list of collections in the path</returns>
            public async Task<CollectionIds> ListCollections(int? pageSize = null, string pageToken = null) {
                HttpResponseMessage response = await client.PostAsync($"v1/projects/{Firebase.ProjectId}/databases/(default)/documents{TrimPath(Path)}:listCollectionIds", new StringContent(new CollectionListIdContent(pageSize, pageToken).ToString()));

                return JsonConvert.DeserializeObject<CollectionIds>(await response.Content.ReadAsStringAsync());
            }
        }

        /// <summary>
        /// This class is for Firestore document
        /// </summary>
        public sealed class Document {
            [JsonProperty]
            private string name;
            [JsonProperty]
            private Dictionary<string, Value> fields;
            [JsonProperty]
            private string createTime;
            [JsonProperty]
            private string updateTime;

            [JsonIgnore]
            public Dictionary<string, Value> Fields => fields;

            private Document() { }

            /// <summary>
            /// This constructor is to create a new document for a document builder
            /// </summary>
            /// <param name="fields">The initial fields of the document, can be null</param>
            internal Document(Dictionary<string, Value> fields = null) => this.fields = fields ?? new Dictionary<string, Value>();
            /// <summary>
            /// This constructor is for creating a new document for a transaction
            /// </summary>
            /// <param name="name">The name to give to the document, can be null</param>
            /// <param name="document">The document to copy his fields from</param>
            internal Document(string name, Document document) {
                this.name = name;
                fields = document.fields;
            }

            [JsonIgnore]
            public string Name => name.Substring(name.LastIndexOf("/") + 1);

            public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);

            /// <summary>
            /// This class is for a document value
            /// </summary>
            public sealed class Value {
                /// <summary>
                /// This enum is for a possible Value types
                /// </summary>
                public enum Type {
                    None,
                    Boolean,
                    Integer,
                    Double,
                    Timestamp,
                    String,
                    Bytes,
                    Reference,
                    GeoPoint,
                    Array,
                    Map
                }

                [JsonIgnore]
                private Type type;
                [JsonProperty("booleanValue")]
                private bool? BooleanValue;
                [JsonProperty("integerValue")]
                private string IntegerValue;
                [JsonProperty("doubleValue")]
                private double? DoubleValue;
                [JsonProperty("timestampValue")]
                private string TimestampValue;
                [JsonProperty("stringValue")]
                private string StringValue;
                [JsonProperty("bytesValue")]
                private string BytesValue;
                [JsonProperty("referenceValue")]
                private string ReferenceValue;
                [JsonProperty("geoPointValue")]
                private LatLon GeoPointValue;
                [JsonProperty("arrayValue")]
                private ArrayValueType ArrayValue;
                [JsonProperty("mapValue")]
                private MapValueType MapValue;

                /// <summary>
                /// Private constructor so the user won't be able to instantiate this class
                /// </summary>
                private Value() { }

                /// <summary>
                /// This constructor is to create a new value and give it's type and value
                /// </summary>
                /// <param name="type">The type of the value</param>
                /// <param name="value">The value of the value</param>
                public Value(Type type, object value) {
                    this.type = type;
                    switch (type) {
                        case Type.Boolean: {
                            if (value is bool b) {
                                BooleanValue = b;
                            } else {
                                throw new ArgumentException("Entered non-boolean value for a boolean type value", nameof(value));
                            }
                            break;
                        }

                        case Type.Integer: {
                            if (int.TryParse(value.ToString(), out int ignoredInt)) {
                                IntegerValue = value.ToString();
                            } else {
                                throw new ArgumentException("Entered non-integer value for a integer type value", nameof(value));
                            }
                            break;
                        }

                        case Type.Double: {
                            if (double.TryParse(value.ToString(), out double d)) {
                                DoubleValue = d;
                            } else {
                                throw new ArgumentException("Entered non-double value for a double type value", nameof(value));
                            }
                            break;
                        }

                        case Type.Timestamp: {
                            if (value is DateTime date) {
                                TimestampValue = new Timestamp(date).ToString();
                            } else {
                                throw new ArgumentException("Entered non-timestamp value for a timestamp type value", nameof(value));
                            }
                            break;
                        }

                        case Type.String: {
                            StringValue = value.ToString();
                            break;
                        }

                        case Type.Bytes: {
                            try {
                                BytesValue = Convert.ToBase64String(Encoding.Unicode.GetBytes(value.ToString()));
                            } catch {
                                throw new ArgumentException("Entered non-bytes value for a bytes type value", nameof(value));
                            }
                            break;
                        }

                        case Type.Reference: {
                            ReferenceValue = value.ToString();
                            break;
                        }

                        case Type.GeoPoint: {
                            if (value is LatLon latlon) {
                                GeoPointValue = latlon;
                            } else {
                                throw new ArgumentException("Entered non-geo-point value for a geo-point type value", nameof(value));
                            }
                            break;
                        }

                        case Type.Array: {
                            if (value is List<Value> array) {
                                ArrayValue = new ArrayValueType(array);
                            } else {
                                throw new ArgumentException("Entered non-array value for an array type value", nameof(value));
                            }
                            break;
                        }

                        case Type.Map: {
                            if (value is Dictionary<string, Value> map) {
                                MapValue = new MapValueType(map);
                            } else {
                                throw new ArgumentException("Entered non-map value for a map type value", nameof(value));
                            }
                            break;
                        }
                    }
                }

                /// <summary>
                /// This function adds a value to the current value whether it is an array or a map
                /// </summary>
                /// <exception cref="NullReferenceException">If the current value is nor array niether map</exception>
                /// <param name="value">The value to add</param>
                /// <param name="name">The name of the value, only if the current value is a map</param>
                public void AddValue(Value value, string name = null) {
                    if (ArrayValue != null) {
                        ArrayValue.AddValue(value);
                    } else if (MapValue != null) {
                        MapValue.AddValue(name, value);
                    } else {
                        throw new TypeAccessException("Type of this value is nor array niether map");
                    }
                }

                /// <summary>
                /// This function is for the json serialization
                /// </summary>
                public bool ShouldSerializeBooleanValue() => type == Type.Boolean || BooleanValue != null;
                /// <summary>
                /// This function is for the json serialization
                /// </summary>
                public bool ShouldSerializeIntegerValue() => type == Type.Integer || IntegerValue != null;
                /// <summary>
                /// This function is for the json serialization
                /// </summary>
                public bool ShouldSerializeDoubleValue() => type == Type.Double || DoubleValue != null;
                /// <summary>
                /// This function is for the json serialization
                /// </summary>
                public bool ShouldSerializeTimestampValue() => type == Type.Timestamp || TimestampValue != null;
                /// <summary>
                /// This function is for the json serialization
                /// </summary>
                public bool ShouldSerializeStringValue() => type == Type.String || StringValue != null;
                /// <summary>
                /// This function is for the json serialization
                /// </summary>
                public bool ShouldSerializeBytesValue() => type == Type.Bytes || BytesValue != null;
                /// <summary>
                /// This function is for the json serialization
                /// </summary>
                public bool ShouldSerializeReferenceValue() => type == Type.Reference || ReferenceValue != null;
                /// <summary>
                /// This function is for the json serialization
                /// </summary>
                public bool ShouldSerializeGeoPointValue() => type == Type.GeoPoint || GeoPointValue != null;
                /// <summary>
                /// This function is for the json serialization
                /// </summary>
                public bool ShouldSerializeArrayValue() => type == Type.Array || ArrayValue != null;
                /// <summary>
                /// This function is for the json serialization
                /// </summary>
                public bool ShouldSerializeMapValue() => type == Type.Map || MapValue != null;

                /// <summary>
                /// This class is for the LatLon value for a value in document
                /// </summary>
                public sealed class LatLon {
                    [JsonProperty]
                    private double latitude;
                    [JsonProperty]
                    private double longitude;

                    public LatLon(double lat, double lon) => (latitude, longitude) = (lat, lon);
                }

                /// <summary>
                /// This class is for the MapValue for a value in document
                /// </summary>
                public sealed class MapValueType {
                    [JsonProperty]
                    private Dictionary<string, Value> fields;

                    public MapValueType(Dictionary<string, Value> fields = null) => this.fields = fields ?? new Dictionary<string, Value>();

                    /// <summary>
                    /// This function adds an entry to the dictionary
                    /// </summary>
                    /// <param name="key">The key of the entry</param>
                    /// <param name="value">The value of the entry</param>
                    public void AddValue(string key, Value value) => fields.Add(key, value);
                }

                /// <summary>
                /// This class is for the ArrayValue for a value in document
                /// </summary>
                public sealed class ArrayValueType {
                    [JsonProperty]
                    private List<Value> values;

                    public ArrayValueType(List<Value> values = null) => this.values = values ?? new List<Value>();

                    /// <summary>
                    /// This function adds an item to the array
                    /// </summary>
                    /// <param name="item">The item to add</param>
                    public void AddValue(Value item) => values.Add(item);
                }
            }
        }

        /// <summary>
        /// This class is to build a document to upload to Firestore
        /// </summary>
        public sealed class DocumentBuilder {
            private Document document;

            public DocumentBuilder(Dictionary<string, Value> fields) => document = new Document(fields);
            public DocumentBuilder() => document = new Document();

            /// <summary>
            /// This function adds a value to the current document builder
            /// </summary>
            /// <param name="key">The name of the value</param>
            /// <param name="value">The value to add</param>
            /// <returns>The current instance of document builder with the field added</returns>
            public DocumentBuilder AddField(string key, Value value) {
                document.Fields.Add(key, value);
                return this;
            }

            /// <summary>
            /// This function adds values to the current document builder
            /// </summary>
            /// <param name="fields">Array of values to add</param>
            /// <returns>The current instance of document builder with the fields added</returns>
            public DocumentBuilder AddFields(params (string key, Value value)[] fields) {
                foreach ((string key, Value value) in fields) {
                    document.Fields.Add(key, value);
                }
                return this;
            }

            public Dictionary<string, Value> Fields => document.Fields;

            /// <summary>
            /// This function clears all the fields of the document
            /// </summary>
            public void Clear() => document.Fields.Clear();

            /// <summary>
            /// This function builds the document
            /// </summary>
            /// <returns>The built document</returns>
            public Document Build() => document;
        }

        /// <summary>
        /// This class is for building a transaction
        /// </summary>
        public sealed class TransactionBuilder {
            [JsonIgnore]
            private string BasePath { get; }
            [JsonProperty]
            private TransactionOptions options;

            public TransactionBuilder(string basePath, TransactionOptions.Mode mode, object param) => (BasePath, options) = (basePath, new TransactionOptions(mode, param));

            /// <summary>
            /// This function starts a transaction
            /// </summary>
            /// <returns>The started transaction</returns>
            public async Task<Transaction> StartTransaction() {
                string authString = (Firebase.GetInstance().FirebaseAuth.GetCurrentUser().idToken ?? null)?.Insert(0, "?access_token=");
                HttpResponseMessage response = await client.PostAsync($"v1/projects/{Firebase.ProjectId}/databases/(default)/documents:beginTransaction{authString}", new StringContent(options.ToString()));

                if (response.IsSuccessStatusCode) {
                    var pattern = new { transaction = "" };
                    return new Transaction(BasePath, JsonConvert.DeserializeAnonymousType(await response.Content.ReadAsStringAsync(), pattern).transaction);
                }
                throw new Exception("Error starting transaction");
            }

            /// <summary>
            /// This class is for a transaction options
            /// </summary>
            public sealed class TransactionOptions {
                public enum Mode {
                    None, ReadOnly, ReadWrite
                }

                [JsonIgnore]
                private Mode mode;

                [JsonProperty("readOnly")]
                private ReadOnlyProperty ReadOnly;
                [JsonProperty("readWrite")]
                private ReadWriteProperty ReadWrite;

                private TransactionOptions() { }

                internal TransactionOptions(Mode mode, object param) {
                    this.mode = mode;
                    if (mode == Mode.ReadOnly && param is DateTime time) {
                        ReadOnly = new ReadOnlyProperty(new Timestamp(time).ToString());
                    } else if (mode == Mode.ReadWrite && param is string transaction) {

                    } else if (mode == Mode.None) {
                        throw new ArgumentException("Mode can't be None", nameof(mode));
                    }
                }

                /// <summary>
                /// This function is for the json serialization
                /// </summary>
                public bool ShouldSerializeReadOnly() => ReadOnly != null;
                /// <summary>
                /// This function is for the json serialization
                /// </summary>
                public bool ShouldSerializeReadWrite() => ReadWrite != null;

                public override string ToString() => JsonConvert.SerializeObject(this);

                /// <summary>
                /// This class is for a readonly transaction
                /// </summary>
                private sealed class ReadOnlyProperty {
                    [JsonProperty]
                    private string readTime;

                    public ReadOnlyProperty(string time) => readTime = time;
                }

                /// <summary>
                /// This class is for a readwrite transaction
                /// </summary>
                private sealed class ReadWriteProperty {
                    [JsonProperty]
                    private string retryTransaction;

                    public ReadWriteProperty(string transaction) => retryTransaction = transaction;
                }
            }
        }

        /// <summary>
        /// This class is for a transaction
        /// </summary>
        public sealed class Transaction {

            public enum Action {
                None, Update, Delete
            }

            [JsonIgnore]
            private string BasePath { get; }
            [JsonProperty]
            private string transaction;
            [JsonProperty]
            private List<Write> writes = new List<Write>();

            private Transaction() { }

            internal Transaction(string basePath, string transaction) => (BasePath, this.transaction) = (basePath, transaction);

            /// <summary>
            /// This function adds a write to the writes list
            /// </summary>
            /// <param name="action">The action, either update or delete</param>
            /// <param name="param">The action parameter</param>
            /// <param name="mask">The update mask</param>
            /// <param name="current">State of current document</param>
            /// <returns>The current object, so you can chain methods</returns>
            public Transaction AddWrite(Action action, object param, string name = null, DocumentMask mask = null, Precondition current = null) {
                writes.Add(new Write(BasePath, action, param, name, mask, current));
                return this;
            }

            /// <summary>
            /// This function commits the transaction to the database
            /// </summary>
            /// <returns><see langword="true"/> if committed successfully, otherwise <see langword="false"/></returns>
            public async Task<bool> Commit() {
                string authString = (Firebase.GetInstance().FirebaseAuth.GetCurrentUser().idToken ?? null)?.Insert(0, "?access_token=");
                HttpResponseMessage response = await client.PostAsync($"v1/projects/{Firebase.ProjectId}/databases/(default)/documents:commit{authString}", new StringContent(ToString()));

                return response.IsSuccessStatusCode;
            }

            public override string ToString() => JsonConvert.SerializeObject(this);

            /// <summary>
            /// This class is for a write for transaction
            /// </summary>
            internal sealed class Write {
                [JsonProperty]
                private DocumentMask updateMask;
                [JsonProperty]
                private Precondition currentDocument;
                [JsonProperty("update")]
                private Document Update;
                [JsonProperty("delete")]
                private string Delete;

                internal Write(string basePath, Action action, object param, string name = null, DocumentMask mask = null, Precondition current = null) {
                    if (action == Action.Update && param is Document updateDoc) {
                        Update = new Document($"{basePath}/{name}", updateDoc);
                    } else if (action == Action.Delete && param is string deleteDoc) {
                        Delete = deleteDoc;
                    } else if (action == Action.None) {
                        throw new ArgumentException("Action can't be None", nameof(action));
                    } else {
                        throw new ArgumentException("Wrong parameter", nameof(param));
                    }
                }

                /// <summary>
                /// This function is for the json serialization
                /// </summary>
                public bool ShouldSerializeUpdate() => Update != null;
                /// <summary>
                /// This function is for the json serialization
                /// </summary>
                public bool ShouldSerializeDelete() => Delete != null;
            }
        }

        /// <summary>
        /// This class is for document mask, eg. for a transaction
        /// </summary>
        public sealed class DocumentMask {
            [JsonProperty]
            private readonly List<string> fieldPaths;

            public DocumentMask(List<string> fieldPaths = null) => this.fieldPaths = fieldPaths;

            public override string ToString() => JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// This class is for a document list that got fetched from a list document call
        /// </summary>
        public sealed class DocumentList {
            [JsonProperty]
            private List<Document> documents;
            [JsonProperty]
            private string nextPageToken;

            private DocumentList() { }

            [JsonIgnore]
            public List<Document> Documents => documents;
            [JsonIgnore]
            public string NextPageToken => nextPageToken;
        }

        /// <summary>
        /// This class is for a collection list request content
        /// </summary>
        public sealed class CollectionListIdContent {
            [JsonProperty("pageSize")]
            private int? PageSize;
            [JsonProperty("pageToken")]
            private string PageToken;

            public CollectionListIdContent(int? pageSize = null, string pageToken = null) => (PageSize, PageToken) = (pageSize, pageToken);

            /// <summary>
            /// This function is for the json serialization
            /// </summary>
            public bool ShouldSerializePageSize() => PageSize != null;
            /// <summary>
            /// This function is for the json serialization
            /// </summary>
            public bool ShouldSerializePageToken() => PageToken != null;

            public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// This class is for a collection list that got fetched from a list collection ids call
        /// </summary>
        public sealed class CollectionIds {
            [JsonProperty]
            private List<string> collectionIds;
            [JsonProperty]
            private string nextPageInfo;

            [JsonIgnore]
            public List<string> GetCollectionIds => collectionIds;
            [JsonIgnore]
            public string NextPageInfo => nextPageInfo;

            public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// This class is for a precondition of a document in the database
        /// </summary>
        public sealed class Precondition {
            private bool? exists = null;
            private string updateTime = null;

            public Precondition(bool exists) => this.exists = exists;
            public Precondition(string updateTime) => this.updateTime = updateTime;

            public (string, object) GetQuery() => exists != null ? ((string, object))("exists", exists) : ("updateTime", updateTime);
        }

        /// <summary>
        /// This class is for a timestamp value for a document
        /// </summary>
        public sealed class Timestamp {

            private DateTime date;

            internal Timestamp(DateTime date) => this.date = date;

            public override string ToString() => TimeZoneInfo.ConvertTimeToUtc(date).ToString();
        }
    }

    #endregion Firestore

    #region Authentication

    /// <summary>
    /// This class is for the Firebase authentication
    /// </summary>
    public sealed class FirebaseAuth {

        private static readonly HttpClient identityClient = new HttpClient {
            BaseAddress = new Uri("https://identitytoolkit.googleapis.com")
        };
        private static readonly HttpClient tokenClient = new HttpClient {
            BaseAddress = new Uri("https://securetoken.googleapis.com")
        };

        private readonly string apiKey;
        private static FirebaseUser firebaseUser;
        public static bool IsLoggedIn => firebaseUser != null;

        internal FirebaseAuth(string apiKey) {
            if (string.IsNullOrEmpty(apiKey)) {
                throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
            }
            identityClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            this.apiKey = apiKey;
        }

        /// <summary>
        /// This function is used to sign in user to Firebase
        /// </summary>
        /// <param name="email">The user's email</param>
        /// <param name="password">The user's password</param>
        /// <returns><see langword="true"/> if successfully signed in, otherwise <see langword="false"/></returns>
        public async Task<bool> SignInWithPasswordEmail(string email, string password) {
            HttpResponseMessage response = await identityClient.PostAsync($"v1/accounts:signInWithPassword?key={apiKey}", new StringContent(UserAuth(email, password)));
            if (response.IsSuccessStatusCode) {
                firebaseUser = JsonConvert.DeserializeObject<FirebaseUser>(await response.Content.ReadAsStringAsync());
                Firestore.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firebaseUser.idToken);
                return true;
            }
            firebaseUser = null;
            return false;
        }

        /// <summary>
        /// This function is used to sign up user to Firebase
        /// </summary>
        /// <param name="email">The user's email</param>
        /// <param name="password">The user's password</param>
        /// <returns><see langword="true"/> if successfully signed up the user, otherwise <see langword="false"/></returns>
        public async Task<FirebaseUser> SignUp(string email, string password) {
            HttpResponseMessage response = await identityClient.PostAsync($"v1/accounts:signUp?key={apiKey}", new StringContent(UserAuth(email, password)));

            return response.IsSuccessStatusCode ? JsonConvert.DeserializeObject<FirebaseUser>(await response.Content.ReadAsStringAsync()) : null;
        }

        /// <summary>
        /// This function send password reset email to a Firebase user
        /// </summary>
        /// <param name="email">The user's email</param>
        /// <returns><see langword="true"/> if the email was sent, otherwise <see langword="false"/></returns>
        public async Task<bool> SendPasswordResetEmail(string email) {
            HttpResponseMessage response = await identityClient.PostAsync($"v1/accounts:sendOobCode?key={apiKey}", new StringContent(JsonConvert.SerializeObject(new { requestType = "PASSWORD_RESET", email })));

            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// This function is used to sign in user with a refresh token granted from signin up a user or signed in user
        /// </summary>
        /// <param name="token">The refresh token</param>
        /// <returns><see langword="true"/> if succeded, otherwise <see langword="false"/></returns>
        public async Task<bool> SignInWithToken(string token) {
            HttpResponseMessage response = await tokenClient.PostAsync($"v1/token?key={apiKey}", new StringContent(JsonConvert.SerializeObject(new { grant_type = "refresh_token", refreshToken = token })));
            if (response.IsSuccessStatusCode) {
                firebaseUser = JsonConvert.DeserializeObject<FirebaseUser>(await response.Content.ReadAsStringAsync());
                Firestore.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firebaseUser.idToken);
                return true;
            }
            firebaseUser = null;
            return false;
        }

        /// <summary>
        /// This function returns the current user
        /// </summary>
        /// <returns>The current user</returns>
        public FirebaseUser GetCurrentUser() => firebaseUser;

        /// <summary>
        /// This function returns a new user for a user request content
        /// </summary>
        /// <param name="email">The user's email</param>
        /// <param name="password">The user's password</param>
        /// <returns>The user in a JSON format</returns>
        internal static string UserAuth(string email, string password) => JsonConvert.SerializeObject(new { email, password, returnSecureToken = true });

        /// <summary>
        /// This class is for a firebase user details
        /// </summary>
        public class FirebaseUser {
            [JsonProperty]
            private string kind;
            public string localId;
            public string email;
            public string displayName;
            [JsonProperty]
            internal string idToken;
            [JsonProperty]
            private bool registered;
            public string refreshToken;
            [JsonProperty]
            private string expiresIn;

            public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

    #endregion Authentication

    #endregion Firebase
#pragma warning restore IDE0044, IDE0051 , IDE0052, 169, 414, 649
}
