using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Bengan {
    public enum SessionSaveMode {
        SaveOnChange = 1,
        SaveManually = 2
    }
    public enum SessionLogMode {
        LogAll = 1,
        LogImportant = 2,
        LogNothing = 3,
    }
    public enum SessionEncrytionMode {
        HardEncryption = 1,
        NoEncryption = 2,
    }
    public static class SessionDataHandler
    {
        private static readonly List<SessionList> sessionLists = new List<SessionList>();
        private static readonly List<VariableData> sessionVariables = new List<VariableData>();
        private static string sessionFilePath;
        private static string fileContent;
        private static SessionSaveMode saveMode;
        private static SessionLogMode logMode;
        private static SessionEncrytionMode encryptionMode;
        public static void ClearData() { 
            File.Delete(Path.Combine(Application.persistentDataPath, "SessionVariables.txt")); 
            if(logMode == SessionLogMode.LogImportant || logMode == SessionLogMode.LogAll) Debug.Log(" -- SessionDataHandler -- Succesfully cleared the session save data.");
            sessionLists.Clear();
            sessionVariables.Clear();
            UpdateFileContent();
        }
        private static string EncryptedString(string input_string)
        {
            StringBuilder encrypted = new StringBuilder();
            for (int i = 0; i < input_string.Length; i++){
                if (input_string[i] == '-') encrypted.Append(input_string[i]);
                else{
                    int shift = (i * i + 3) % 128; // This makes the pattern more complex.
                    int encryptedChar = (int)input_string[i] + shift;
                    // Ensuring that the encrypted character is within the ASCII range (0-127).
                    encryptedChar = encryptedChar % 128;
                    encrypted.Append((char)encryptedChar);
                }
            }
            return encrypted.ToString();
        }
        private static string DecryptedString(string encrypted_string)
        {
            StringBuilder decrypted = new StringBuilder();
            for (int i = 0; i < encrypted_string.Length; i++){
                if (encrypted_string[i] == '-')decrypted.Append(encrypted_string[i]);
                else{
                    int shift = (i * i + 3) % 128; // This must match the pattern from the EncryptedString method.
                    int originalChar = (int)encrypted_string[i] - shift;
                    // Ensuring that the original character is within the ASCII range (0-127).
                    if (originalChar < 0){
                        originalChar += 128;
                    }
                    decrypted.Append((char)originalChar);
                }
            }
            return decrypted.ToString();
        }
        public static void Initialize(SessionLogMode logType, SessionEncrytionMode encryptionType) { Initialize(SessionSaveMode.SaveOnChange, logType, encryptionType); }
        public static void Initialize(SessionEncrytionMode encryptionType) { Initialize(SessionSaveMode.SaveOnChange, SessionLogMode.LogNothing, encryptionType); }
        public static void Initialize(SessionSaveMode saveType, SessionEncrytionMode encryptionType) { Initialize(saveType, SessionLogMode.LogNothing, encryptionType); }
        public static void Initialize(SessionSaveMode saveType = SessionSaveMode.SaveOnChange, SessionLogMode logType = SessionLogMode.LogNothing, SessionEncrytionMode encryptionType = SessionEncrytionMode.NoEncryption)
        {
            //set variable
            saveMode = saveType;
            logMode = logType;
            encryptionMode = encryptionType;
            if(logMode == SessionLogMode.LogImportant || logMode == SessionLogMode.LogAll) Debug.Log(" -- SessionDataHandler -- Successfully initialized");
        }
        public static void OpenFile(string file_name) {
            file_name = file_name += ".txt";
            sessionVariables.Clear();
            sessionLists.Clear();
            
            //set filepath
            sessionFilePath = Path.Combine(Application.persistentDataPath, file_name);
            UpdateFileContent();
            LoadSessionData();
        }
        public static string GetStringFromList(string listName, int index) {
            SessionList currentList = GetList(listName);
            foreach (var listData in currentList.data) {
                if (listData.index == index) {
                    return listData.content;
                }
            }
            throw new Exception($"Index {index} was not assigned in {listName}");
        }
        public static int GetIntFromList(string listName, int index) {
            SessionList currentList = GetList(listName);
            foreach (var listData in currentList.data) {
                if (listData.index == index) {
                    return Convert.ToInt32(listData.content);
                }
            }
            throw new Exception($"Index {index} was not assigned in {listName}");
        }
        public static void RemoveAt(string listName, int index) {
            var list = GetList(listName);
            if (index >= list.data.Count || index < 0) {
                if(logMode == SessionLogMode.LogImportant || logMode == SessionLogMode.LogAll) Debug.LogError($" -- SessionDataHandler -- Index {index} is out of range for the list {listName}.");
                return;
            }
            list.data.RemoveAt(index);
            for (int i = index; i < list.data.Count; i++) {
                list.data[i] = new SessionListData() {
                    index = i,
                    variable_indexer = list.data[i].variable_indexer,
                    content = list.data[i].content
                };
            }
            if(logMode == SessionLogMode.LogAll) Debug.Log($" -- SessionDataHandler -- Successfully removed data at index {index} in list {listName}.");
            if (saveMode == SessionSaveMode.SaveOnChange) SaveSessionData();
            UpdateFileContent();
        }
        public static int GetIndexOfString(string listName, string value) {
            var list = GetList(listName);
            for (int i = 0; i < list.data.Count; i++) {
                if (list.data[i].variable_indexer == 0 && list.data[i].content == value) {
                    return i;
                }
            }
            return -1; // Return -1 if the value is not found
        }
        public static int GetIndexOfInt(string listName, int value) {
            var list = GetList(listName);
            for (int i = 0; i < list.data.Count; i++) {
                if (list.data[i].variable_indexer == 1 && Convert.ToInt32(list.data[i].content) == value) {
                    return i;
                }
            }
            return -1; // Return -1 if the value is not found
        }
        public static int GetIndexOfFloat(string listName, float value) {
            var list = GetList(listName);
            for (int i = 0; i < list.data.Count; i++) {
                if (list.data[i].variable_indexer == 2 && Mathf.Approximately(Convert.ToSingle(list.data[i].content), value)) {
                    return i;
                }
            }
            return -1; // Return -1 if the value is not found
        }
        public static void RemoveFloatFromList(string listName, float content, bool removeAllOccurrences) {
            var list = GetList(listName);
            int removedCount = 0;

            for (int i = 0; i < list.data.Count; i++) {
                if (list.data[i].variable_indexer != 2 || Convert.ToSingle(list.data[i].content) != content) continue;
                list.data.RemoveAt(i);
                removedCount++;
                i--;

                if (!removeAllOccurrences) break;
            }
            UpdateIndexesAndSave(list, listName, removedCount, content, removeAllOccurrences);
        }
        public static void RemoveIntFromList(string listName, int content, bool removeAllOccurrences)
        {
            var list = GetList(listName);
            int removedCount = 0;
            bool removed = false; // Add this flag

            for (int i = 0; i < list.data.Count; i++)
            {
                if (list.data[i].variable_indexer != 1 || Convert.ToInt32(list.data[i].content) != content) continue;

                list.data.RemoveAt(i);
                removedCount++;
                i--;

                removed = true; // Set the flag to true when an element is removed

                if (!removeAllOccurrences && removed) break; // Check if the flag is true and removeAllOccurrences is false
            }
            UpdateIndexesAndSave(list, listName, removedCount, content, removeAllOccurrences);
        }
        public static void RemoveStringFromList(string listName, string content, bool removeAllOccurrences) {
            var list = GetList(listName);
            int removedCount = 0;
            for (int i = 0; i < list.data.Count; i++) {
                if (list.data[i].variable_indexer != 0 || list.data[i].content != content) continue;
                list.data.RemoveAt(i);
                removedCount++;
                i--;

                if (!removeAllOccurrences) break;
            }
            UpdateIndexesAndSave(list, listName, removedCount, content, removeAllOccurrences);
        }
        private static void UpdateIndexesAndSave(SessionList list, string listName, int removedCount, object content, bool removeAllOccurrences)
        {
            for (int i = 0; i < list.data.Count; i++)
            {
                list.data[i] = new SessionListData()
                {
                    index = i,
                    variable_indexer = list.data[i].variable_indexer,
                    content = list.data[i].content
                };
            }

            if (removedCount > 0)
            {
                if(logMode == SessionLogMode.LogAll) Debug.Log($" -- SessionDataHandler -- Successfully removed {removedCount} occurrences of content '{content}' from list {listName}.");
                if (saveMode == SessionSaveMode.SaveOnChange) SaveSessionData();
                UpdateFileContent();
            }
            else if(logMode == SessionLogMode.LogImportant || logMode == SessionLogMode.LogAll) Debug.LogWarning($" -- SessionDataHandler -- Content '{content}' was not found in list {listName}.");

            // Exit early when the first occurrence is removed and removeAllOccurrences is set to false
            if (!removeAllOccurrences && removedCount > 0) return;
        }
        public static float GetFloatFromList(string listName, int index) {
            SessionList currentList = GetList(listName);
            foreach (var listData in currentList.data) {
                if (listData.index == index) {
                    return Convert.ToSingle(listData.content);
                }
            }
            throw new Exception($"Index {index} was not assigned in {listName}");
        }
        public static void CreateNewList(string listName) {
            if (sessionLists.Any(existingList => existingList.list_name == listName)) {
                if(logMode == SessionLogMode.LogImportant || logMode == SessionLogMode.LogAll) Debug.LogWarning($" -- SessionDataHandler -- List named -{listName}- already exists! (Aborting)");
                return;
            }
            sessionLists.Add(new SessionList { list_index = sessionLists.Count, list_name = listName, data = new List<SessionListData>()});
            SaveSessionData();
            UpdateFileContent();
        }
        public static int GetListLength(string listName) {
            SessionList currentList = GetList(listName);
            return currentList.data.Count;
        }
        public static void RemoveList(string listName) {
            var list = GetList(listName);
            if (!sessionLists.Remove(list)) return;
            if (saveMode == SessionSaveMode.SaveOnChange) SaveSessionData(); // Add this line
            UpdateFileContent();
            if(logMode == SessionLogMode.LogAll) Debug.Log($" -- SessionDataHandler -- successfully removed list named {listName}");
        }
        private static SessionList GetList(string listName) {
            foreach (var existingList in sessionLists.Where(existingList => existingList.list_name == listName)) return existingList;
            throw new Exception($"No list named {listName} was found");
        }
        public static void AddStringToList(string listName, string writingContent) {
            var list = GetList(listName);
            list.data.Add(new SessionListData(){index = list.data.Count, content = writingContent, variable_indexer = 0});
            if(logMode == SessionLogMode.LogAll) Debug.Log($" -- SessionDataHandler -- Successfully added -{writingContent}- in -{listName}-");
            if(saveMode == SessionSaveMode.SaveOnChange) SaveSessionData();
            UpdateFileContent();
        }
        public static void AddFloatToList(string listName, float writingContent) {
            var list = GetList(listName);
            list.data.Add(new SessionListData(){index = list.data.Count, content = Convert.ToString(writingContent), variable_indexer = 2});
            if(logMode == SessionLogMode.LogAll) Debug.Log($" -- SessionDataHandler -- Successfully added -{writingContent}- in -{listName}-");
            if(saveMode == SessionSaveMode.SaveOnChange) SaveSessionData();
            UpdateFileContent();
        }
        public static void AddIntToList(string listName, int writingContent) {
            var list = GetList(listName);
            list.data.Add(new SessionListData(){index = list.data.Count, content = Convert.ToString(writingContent), variable_indexer = 1});
            if(logMode == SessionLogMode.LogAll) Debug.Log($" -- SessionDataHandler -- Successfully added -{writingContent}- in -{listName}-");
            if(saveMode == SessionSaveMode.SaveOnChange) SaveSessionData();
            UpdateFileContent();
        }
        public static void ConsoleContent()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(" -- SessionDataHandler -- (Press here to view)");
            sb.AppendLine("");
            sb.AppendLine("Logged saved lists:");

            // Log session lists
            int listIndex = 1;
            foreach (var list in sessionLists)
            {
                sb.AppendLine($"List {listIndex}: {list.list_name}");

                int dataIndex = 1;
                foreach (var data in list.data)
                {
                    string dataType = "";
                    switch (data.variable_indexer)
                    {
                        case 0:
                            dataType = "string";
                            break;
                        case 1:
                            dataType = "integer";
                            break;
                        case 2:
                            dataType = "float";
                            break;
                    }

                    sb.AppendLine($"  Data {dataIndex}: (Index: {data.index}, Content: {data.content}, Type: {dataType})");
                    dataIndex++;
                }
                listIndex++;
            }

            // Log session variables
            sb.AppendLine("\nLogged saved variables:");
            int variableIndex = 1;
            foreach (var VARIABLE in sessionVariables)
            {
                string dataType = "";
                switch (VARIABLE.variable_indexer)
                {
                    case 0:
                        dataType = "string";
                        break;
                    case 1:
                        dataType = "integer";
                        break;
                    case 2:
                        dataType = "float";
                        break;
                }

                sb.AppendLine($"Variable {variableIndex}: {VARIABLE.variable_name} (Content: {VARIABLE.content}, Type: {dataType})");
                variableIndex++;
            }

            Debug.Log(sb.ToString());
        }
        public static void LogFileContent() { Debug.Log(fileContent); }
        public static string GetDataTypeAtIndex(string listName, int index) {
            foreach (var existingList in sessionLists.Where(existingList => existingList.list_name == listName)) {
                switch (existingList.data[index].variable_indexer) {
                    case 0:
                        return "string";
                    case 1:
                        return "integer";
                    case 2:
                        return "float";
                }
            }

            throw new Exception($"No list named {listName} was found");
        }
        public static void SaveSession() {
            SaveSessionData();
        }
        private static void SaveSessionData() {
            //add lists
            string currentString = "";
            foreach (var data in sessionLists) {
                currentString += $"^{data.list_name}]";
                foreach (var dataList in data.data) currentString += $"{dataList.variable_indexer}.{dataList.content}]";
            }
            //add variables
            currentString += "-";
            foreach (var data in sessionVariables) {
                currentString += $"^{data.variable_name}]";
                currentString += $"{data.variable_indexer}.{data.content}]";
            }
            if (encryptionMode == SessionEncrytionMode.HardEncryption) currentString = EncryptedString(currentString);
            File.WriteAllText(sessionFilePath, currentString);
            if(logMode == SessionLogMode.LogImportant || logMode == SessionLogMode.LogAll) Debug.Log(" -- SessionDataHandler -- Session data handler successfully saved.");
        }
        public static void MakeEncrypted() {
            //add lists
            string currentString = "";
            foreach (var data in sessionLists) {
                currentString += $"^{data.list_name}]";
                foreach (var dataList in data.data) currentString += $"{dataList.variable_indexer}.{dataList.content}]";
            }
            //add variables
            currentString += "-";
            foreach (var data in sessionVariables) {
                currentString += $"^{data.variable_name}]";
                currentString += $"{data.variable_indexer}.{data.content}]";
            }
            currentString = EncryptedString(currentString);
            File.WriteAllText(sessionFilePath, currentString);
            if(logMode == SessionLogMode.LogImportant || logMode == SessionLogMode.LogAll) Debug.Log(" -- SessionDataHandler -- Session data handler successfully saved.");
        }
        public static void MakeDecrypted() {
            //add lists
            string currentString = "";
            foreach (var data in sessionLists) {
                currentString += $"^{data.list_name}]";
                foreach (var dataList in data.data) currentString += $"{dataList.variable_indexer}.{dataList.content}]";
            }
            //add variables
            currentString += "-";
            foreach (var data in sessionVariables) {
                currentString += $"^{data.variable_name}]";
                currentString += $"{data.variable_indexer}.{data.content}]";
            }
            currentString = currentString;
            File.WriteAllText(sessionFilePath, currentString);
            if(logMode == SessionLogMode.LogImportant || logMode == SessionLogMode.LogAll) Debug.Log(" -- SessionDataHandler -- Session data handler successfully saved.");
        }
        public static float GetVarFloat(string variableName, float default_value = 0f) {
            foreach (var VARIABLE in sessionVariables) {
                if (VARIABLE.variable_name != variableName) continue;
                //found variable
                if (VARIABLE.variable_indexer == 2) {
                    //right datatype
                    return Convert.ToSingle(VARIABLE.content);
                }
                throw new Exception($"Variable {variableName} is not a integer");
            }
            SetVarFloat(variableName,default_value);
            return default_value;
        }
        public static string GetVarString(string variableName, string default_value = "") {
            foreach (var VARIABLE in sessionVariables) {
                if (VARIABLE.variable_name != variableName) continue;
                //found variable
                if (VARIABLE.variable_indexer == 0) {
                    //right datatype
                    return VARIABLE.content;
                }
                throw new Exception($"Variable {variableName} is not a string");
            }
            SetVarString(variableName,default_value);
            return default_value;
        }
        public static int GetVarInt(string variableName, int default_value = 0) {
            foreach (var VARIABLE in sessionVariables) {
                if (VARIABLE.variable_name != variableName) continue;
                //found variable
                if (VARIABLE.variable_indexer == 1) {
                    //right datatype
                    return Convert.ToInt32(VARIABLE.content);
                }
                throw new Exception($"Variable {variableName} is not a float");
            }
            SetVarInt(variableName,default_value);
            return default_value;
        }
        public static void SetVarFloat(string variableName, float value) {
            foreach (var VARIABLE in sessionVariables) {
                if (VARIABLE.variable_name != variableName) continue;
                sessionVariables.Remove(VARIABLE);
                sessionVariables.Add(new VariableData() { content = value.ToString(), variable_indexer = 2, variable_name = variableName });
                if(saveMode == SessionSaveMode.SaveOnChange) SaveSessionData();
                UpdateFileContent();
                if(logMode == SessionLogMode.LogAll) Debug.Log($" -- SessionDataHandler -- Successfully changed variable -{variableName}- to {value}.");
                return;
            }
            sessionVariables.Add(new VariableData(){content = value.ToString(), variable_indexer = 2, variable_name = variableName});
            if(saveMode == SessionSaveMode.SaveOnChange) SaveSessionData();
            UpdateFileContent();
            if(logMode == SessionLogMode.LogAll) Debug.Log($" -- SessionDataHandler -- Successfully created new variable -{variableName} = {value}-.");
        }
        public static void SetVarInt(string variableName, int value) {
            foreach (var VARIABLE in sessionVariables) {
                if (VARIABLE.variable_name != variableName) continue;
                sessionVariables.Remove(VARIABLE);
                sessionVariables.Add(new VariableData() { content = value.ToString(), variable_indexer = 1, variable_name = variableName });
                if(saveMode == SessionSaveMode.SaveOnChange) SaveSessionData();
                UpdateFileContent();
                if(logMode == SessionLogMode.LogAll) Debug.Log($" -- SessionDataHandler -- Successfully changed variable -{variableName}- to {value}.");
                return;
            }
            sessionVariables.Add(new VariableData(){content = value.ToString(), variable_indexer = 1, variable_name = variableName});
            if(saveMode == SessionSaveMode.SaveOnChange) SaveSessionData();
            UpdateFileContent();
            if(logMode == SessionLogMode.LogAll) Debug.Log($" -- SessionDataHandler -- Successfully created new variable -{variableName} = {value}-.");
        }
        public static void SetVarString(string variableName, string value) {
            foreach (var VARIABLE in sessionVariables) {
                if (VARIABLE.variable_name != variableName) continue;
                sessionVariables.Remove(VARIABLE);
                sessionVariables.Add(new VariableData() { content = value.ToString(), variable_indexer = 0, variable_name = variableName });
                if(saveMode == SessionSaveMode.SaveOnChange) SaveSessionData();
                UpdateFileContent();
                if(logMode == SessionLogMode.LogAll) Debug.Log($" -- SessionDataHandler -- Successfully changed variable -{variableName}- to {value}.");
                return;
            }
            sessionVariables.Add(new VariableData(){content = value.ToString(), variable_indexer = 0, variable_name = variableName});
            if(saveMode == SessionSaveMode.SaveOnChange) SaveSessionData();
            UpdateFileContent();
            if(logMode == SessionLogMode.LogAll) Debug.Log($" -- SessionDataHandler -- Successfully created new variable -{variableName} = {value}-.");
        }
        private static void LoadSessionData() {
            bool encrypted = encryptionMode == SessionEncrytionMode.HardEncryption;
            if (encrypted) fileContent = DecryptedString(fileContent);
            
            //lists
            string[] contentDataHolder = fileContent.Split('-');
            string[] listsDataHolder = contentDataHolder[0].Split(']');
            string[] variableDataHolder = contentDataHolder[1].Split('^');
            
            //variables
            if (variableDataHolder.Length > 1) {
                for (int i = 1; i < variableDataHolder.Length; i++) {
                    string[] variableDataSaver = variableDataHolder[i].Split(']');
                    string[] variableContentSaver = variableDataSaver[1].Split('.');
                    sessionVariables.Add(new VariableData(){variable_name = variableDataSaver[0], content = variableContentSaver[1], variable_indexer = Convert.ToInt16(variableContentSaver[0])});
                }
            }
            
            //lists
            if (listsDataHolder.Length > 2) {
                int currentListIndex = 0;
                int currentIndexInList = 0;
                SessionList currentList = new SessionList { list_index = currentListIndex, data = new List<SessionListData>()};
                for (int i = 1; i < listsDataHolder.Length; i++) {
                    //name the string
                    if (i == 1) currentList.list_name = listsDataHolder[0].Split('^')[1];
                    //add new index of data to current list
                    if (listsDataHolder[i].Length > 0) if (listsDataHolder[i][0] != '^') {
                        string[] listData = listsDataHolder[i].Split('.');
                        currentList.data.Add(new SessionListData(){content = listData[1], index = currentIndexInList, variable_indexer = Convert.ToInt32(listData[0])});
                        currentIndexInList++;
                    }
                    //finalize current list
                    if (listsDataHolder[i].Length != 0 && listsDataHolder[i][0] != '^') continue;
                    currentIndexInList = 0;
                    currentListIndex++;
                    sessionLists.Add(currentList);
                    currentList = new SessionList { list_index = currentListIndex, data = new List<SessionListData>()};
                    if (listsDataHolder[i].Length <= 0) continue;
                    currentList.list_name = listsDataHolder[i].Split('^')[1];
                }   
            }
            
            if (encrypted) fileContent = EncryptedString(fileContent);
        }
        private static void UpdateFileContent() {
            //create file
            if (File.Exists(sessionFilePath)) fileContent = File.ReadAllText(sessionFilePath);
            else {
                const string defaultData = "-";
                File.WriteAllText(sessionFilePath, defaultData);
                fileContent = defaultData;
            }
        }
        private struct SessionList {
            public int list_index;
            public string list_name;
            public List<SessionListData> data;
        }
        private struct SessionListData {
            public int index;
            public int variable_indexer;
            public string content;
        }
        private struct VariableData {
            public string variable_name;
            public int variable_indexer;
            public string content;
        }
    }
    public static class BenganGPT
    {
        private static HttpClient Http = new HttpClient();
        public static async Task<string> AskGPT(string question){
            //insert apikey below
            const string apiKey = "...";
            Http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            var jsonContent = new{
                prompt = question,
                model = "text-davinci-003",
                max_tokens = 1000
            };
            var responseContent = await Http.PostAsync("https://api.openai.com/v1/completions", new StringContent(JsonConvert.SerializeObject(jsonContent), Encoding.UTF8, "application/json"));
            var resContext = await responseContent.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<OpenAIResponse>(resContext);
            if (data.error != null){
                return "An error occurred while generating lorem ipsum text: " + data.error.message;
            }
            return data.choices[0].text;
        }
        private class OpenAIResponse{
            public Choice[] choices;
            public Error error;
        }
        private class Choice{
            public string text;
        }
        private class Error{
            public string message;
        }  
    }
    public class BenganMono<T> : MonoBehaviour where T : MonoBehaviour
    {
        [SerializeField] private bool isSingleton = false;
        public static T Instance { get; private set; }
        protected virtual void Awake() {
            if (!isSingleton) return;
            if (Instance == null){
                Instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this) Destroy(gameObject);
        }
        protected void TryInvokeMethod(string method_name)
        {
            var hardCalledMethod = this.GetType().GetMethod(method_name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (hardCalledMethod != null) hardCalledMethod.Invoke(this, null);
            else Debug.LogWarning($"Method named {method_name} does not exist. (aborting)");
        }
    }
    public class BenganVolumeMono<T> : MonoBehaviour where T : MonoBehaviour
    {
        private bool isSingleton = true;
        public static T Instance { get; private set; }
        protected virtual void Awake() {
            if (!isSingleton) return;
            if (Instance == null){
                Instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this) Destroy(gameObject);
        }
    }
}
