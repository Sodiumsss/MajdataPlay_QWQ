using MajdataPlay.IO;
using MajdataPlay.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Directory = System.IO.Directory;
using File = System.IO.File;

public class Canvas : MonoBehaviour
{
    private static string ip = "server.qwq.ski";     // 设置服务器 IP
    private static string port = ""; // 设置端口号
    private static string pre = "https://";

    private bool hasLogined = false;
    private bool hasRandomed = false;


    public ListManager listManager;
    public GameObject page;
    public GameObject otherPanel;

    public GameObject loginPage;
    public GameObject homePage;
    public GameObject loadingPage;

    public GameObject random1;
    public GameObject random2;
    public GameObject search1;

    public GameObject username;
    public GameObject password;
    public GameObject loginButton;
    public GameObject randomButton;
    public GameObject loadingButton;

    public Image RandomImage1;
    public Image RandomImage2;

    private TMP_InputField Username;
    private TMP_InputField Password;
    private TextMeshProUGUI LoadingText;

    private DataItem randomData1;
    private DataItem randomData2;

    [System.Serializable]
    public class LoginData
    {
        public string username;
        public string password;
    }
    [System.Serializable]
    public class DataItem
    {
        public int id;
        public string name;
        public string photo;
        public string description;
        public string maiDataLevels;
        public int zipDataId;
        public string bvCode;
        public string downloadUrl;
        public string owner;
        public int state;
        public int likes;
        public float hot;
        public bool ilike;
    }

    [System.Serializable]
    public class DataArray
    {
        public DataItem[] dataItems;
    }
    public class R
    {
        public int code { get; set; }
        public string message { get; set; }
        public object data { get; set; }
        public string GetString()
        {
            string str = $"code:{this.code},message:{this.message},data:{(this.data != null ? this.data.ToString() : "null")}";
            return str;
        }

        public bool IsSuccess()
        {
            return code != 0;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        page.SetActive(false);
        random1.SetActive(false);
        loadingPage.SetActive(false);
        loadingButton.SetActive(false);
        random2.SetActive(false);
        Username = username.GetComponent<TMP_InputField>();
        Password = password.GetComponent<TMP_InputField>();
        Transform loadingTextTrans = loadingPage.transform.Find("LoadingText");
        LoadingText = loadingTextTrans.GetComponent<TextMeshProUGUI>();
        Debug.Log(LoadingText);
        Username.text = PlayerPrefs.HasKey("Username") ? PlayerPrefs.GetString("Username") : "";
        Password.text = PlayerPrefs.HasKey("Password") ? PlayerPrefs.GetString("Password") : "";
    }

    public void OnLoginButtonClick()
    {
        LoginData loginData = new() { username = Username.text, password = Password.text };
        PlayerPrefs.SetString("Username", Username.text);
        PlayerPrefs.SetString("Password", Password.text);
        string json = JsonConvert.SerializeObject(loginData);
        StartCoroutine(ApiPost("user", "login", json, 8000, HandleLoginResult));
    }

    public void OnRandomButtonClick()
    {
        loadingPage.SetActive(true);
        homePage.SetActive(false);
        StartCoroutine(ApiGet("mai-data", "getRandom", null, 8000, HandleRandomResult));
    }
    public static List<List<object>> ParseJsonString(string jsonString)
    {
        // 解析 JSON 字符串
        var data = JsonConvert.DeserializeObject<List<List<object>>>(jsonString);

        // 创建结果列表
        var result = new List<List<object>>();

        // 遍历每个子列表
        foreach (var item in data)
        {
            var textObject = item[0] as JObject;
            var text = textObject["text"].ToString();
            var value = Convert.ToDouble(item[1].ToString());

            // 添加到结果中
            result.Add(new List<object> { text, value });
        }

        return result;
    }

    public void GetChart1()
    {
        SetLoadingText("正在下载中……");
        StartCoroutine(ApiGet("file", "getPath", "fileId=" + randomData1.zipDataId, 8000, HandleGetChartURL));

    }
    public void GetChart2()
    {
        SetLoadingText("正在下载中……");
        StartCoroutine(ApiGet("file", "getPath", "fileId=" + randomData2.zipDataId, 8000, HandleGetChartURL));

    }

    void HandleGetChartURL(R r)
    {
        if (r != null && r.message != null && r.IsSuccess())
        {
            var url = pre + ip + r.message;
            loadingPage.SetActive(true);
            homePage.SetActive(false);
            Debug.Log(url);
            SetLoadingText("正在解压中……");
            StartCoroutine(DownloadAndExtractZip(url, Guid.NewGuid().ToString()));
        }
    }
    public void OnSearchDownloadButtonClick()
    {
        Transform searchTrans = search1.transform.Find("SearchField");
        TMP_InputField tmp = searchTrans.GetComponent<TMP_InputField>();
        if (tmp != null)
        {
            loadingPage.SetActive(true);
            homePage.SetActive(false);
            var text = tmp.text;
            if (text != null && text != "")
            {
                if (text.Length == 1)
                {
                    SetLoadingText("长度错误，请重新输入。");
                    loadingButton.SetActive(true);
                    return;
                }
                if (text.StartsWith("G") || text.StartsWith("Z"))
                {
                    var kind = text[..1];
                    var id = text[1..];
                    if (kind == "G")
                    {
                        StartCoroutine(ApiGet("mai-data", "getDownloadURL", "kind=1&id=" + id, 8000, HandleSearchResult));

                    }
                    else
                    {
                        StartCoroutine(ApiGet("mai-data", "getDownloadURL", "kind=2&id=" + id, 8000, HandleSearchResult));
                    }

                }
                else
                {
                    SetLoadingText("开头类型错误，仅能为G或Z。");
                    loadingButton.SetActive(true);
                }
            }
            else
            {
                SetLoadingText("输入框不能為空。");
                loadingButton.SetActive(true);
            }
        }
    }
    void HandleSearchResult(R r)
    {
        if (r != null && r.message != null && r.IsSuccess())
        {
            var url = pre + ip  + r.message;
            SetLoadingText("正在解压中……");
            StartCoroutine(DownloadAndExtractZip(url, Guid.NewGuid().ToString()));
        }
    }
    public IEnumerator DownloadAndExtractZip(string url, string zipFileName)
    {
        // 构建路径：游戏主目录下的 MaiCharts\default 文件夹
        string gameRootPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "MaiCharts", "default");

        // 确保 MaiCharts\default 路径存在，如果不存在则创建
        if (!Directory.Exists(gameRootPath))
        {
            Directory.CreateDirectory(gameRootPath);
        }

        // 保存 ZIP 文件的完整路径
        string zipFilePath = Path.Combine(gameRootPath, zipFileName);
        Debug.Log(zipFilePath);

        // 下载 ZIP 文件
        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = 10;

        // 发送请求并等待响应
        yield return request.SendWebRequest();

        Debug.Log("downloadurl:" + url);

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + request.error);
            SetLoadingText("压缩包下载出错。");
        }
        else
        {
            // 将 ZIP 文件保存到指定位置
            File.WriteAllBytes(zipFilePath + ".zip", request.downloadHandler.data);

            string folderName = Path.GetFileNameWithoutExtension(zipFileName);
            string extractPath = Path.Combine(gameRootPath, folderName);

            // 如果文件夹已存在，删除并重新创建
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true); // 递归删除目录
            }

            // 创建临时解压目录
            string tempExtractPath = Path.Combine(gameRootPath, "temp_" + folderName);
            if (Directory.Exists(tempExtractPath))
            {
                Directory.Delete(tempExtractPath, true);
            }

            // 解压 ZIP 文件到临时目录
            ZipFile.ExtractToDirectory(zipFilePath + ".zip", tempExtractPath);

            // 检查 ZIP 文件内容是否包含文件夹
            bool containsFolder = false;
            foreach (string entry in Directory.GetDirectories(tempExtractPath))
            {
                containsFolder = true;
                break;
            }

            // 如果包含文件夹，提取文件夹中的所有文件到目标路径
            if (containsFolder)
            {
                // 获取第一个文件夹的路径
                string[] directories = Directory.GetDirectories(tempExtractPath);
                if (directories.Length > 0)
                {
                    string innerFolderPath = directories[0]; // 假设只有一个子文件夹
                    foreach (string file in Directory.GetFiles(innerFolderPath, "*", SearchOption.AllDirectories))
                    {
                        string relativePath = file.Substring(innerFolderPath.Length + 1); // 相对路径
                        string targetPath = Path.Combine(extractPath, relativePath);

                        // 确保目标路径的文件夹存在
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                        File.Move(file, targetPath);
                    }
                }
            }
            else
            {
                // 否则按照原逻辑进行解压
                Directory.Move(tempExtractPath, extractPath);
            }

            // 删除临时文件夹
            if (Directory.Exists(tempExtractPath))
            {
                Directory.Delete(tempExtractPath, true);
            }

            File.Delete(zipFilePath + ".zip");
            Debug.Log("ZIP file extracted to: " + extractPath);
            SetLoadingText("解压成功。");
        }
        loadingButton.SetActive(true);
    }
    void SetLoadingText(string text)
    {
        if (LoadingText != null)
        {
            Debug.Log("set");
            LoadingText.text = text;
        }
    }
    void ResetLoadingText()
    {
        SetLoadingText("正在加载中……");
    }


    void HandleRandomResult(R r)
    {
        if (r != null && r.data != null && r.IsSuccess())
        {
            string json = r.data.ToString();
            JArray jsonArray = JArray.Parse(json);

            int index = 0;

            foreach (JObject obj in jsonArray.Cast<JObject>())
            {

                DataItem item = obj.ToObject<DataItem>();
                Debug.Log($"ID: {item.id}");
                Debug.Log($"Name: {item.name}");
                Debug.Log($"Photo URL: {item.photo}");
                Debug.Log($"Level: {item.maiDataLevels}");
                // 解析 JSON 为 JArray
                var result = ParseJsonString(item.maiDataLevels);

                // 打印输出
                var levelStr = "";
                foreach (var v in result)
                {
                    var temp = string.Join(",", v);
                    levelStr += temp + "|";
                }
                levelStr = levelStr[..^1];

                if (index == 0)
                {
                    randomData1 = item;
                    StartCoroutine(LoadImageFromURL(item.photo, HandleRandom1Photo));
                    Transform random1NameTrans = random1.transform.Find("RandomName1");
                    Transform random1DesignerTrans = random1.transform.Find("RandomDesigner1");
                    Transform random1LevelTrans = random1.transform.Find("RandomLevel1");

                    if (random1NameTrans != null)
                    {
                        if (random1NameTrans.TryGetComponent<TextMeshProUGUI>(out var random1Name))
                        {
                            random1Name.text = item.name;
                        }
                    }
                    if (random1DesignerTrans != null)
                    {
                        if (random1DesignerTrans.TryGetComponent<TextMeshProUGUI>(out var random))
                        {
                            random.text = item.owner;
                        }
                    }
                    if (random1LevelTrans != null)
                    {
                        if (random1LevelTrans.TryGetComponent<TextMeshProUGUI>(out var random))
                        {
                            random.text = levelStr;
                        }
                    }
                    random1.SetActive(true);
                    index++;
                }
                else if (index == 1)
                {
                    randomData2 = item;
                    StartCoroutine(LoadImageFromURL(item.photo, HandleRandom2Photo));
                    Transform random2NameTrans = random2.transform.Find("RandomName2");
                    Transform random2DesignerTrans = random2.transform.Find("RandomDesigner2");
                    Transform random2LevelTrans = random2.transform.Find("RandomLevel2");

                    if (random2NameTrans != null)
                    {
                        if (random2NameTrans.TryGetComponent<TextMeshProUGUI>(out var random))
                        {
                            random.text = item.name;
                        }
                    }
                    if (random2DesignerTrans != null)
                    {
                        if (random2DesignerTrans.TryGetComponent<TextMeshProUGUI>(out var random))
                        {
                            random.text = item.owner;
                        }
                    }
                    if (random2LevelTrans != null)
                    {
                        if (random2LevelTrans.TryGetComponent<TextMeshProUGUI>(out var random))
                        {
                            random.text = levelStr;
                        }
                    }
                    random2.SetActive(true);
                }
            }
            SetLoadingText("加载完成。");
            if (loadingButton != null)
            {
                loadingButton.SetActive(true);
            }
        }
        else
        {
            SetLoadingText("加载失败。");
            if (loadingButton != null)
            {
                loadingButton.SetActive(true);
            }
        }


    }
    public void OnLoadingButtonClick()
    {
        loadingPage.SetActive(false);
        homePage.SetActive(true);
        loadingButton.SetActive(false);
        ResetLoadingText();
        if (!PlayerPrefs.HasKey("QWQToken"))
        {
            homePage.SetActive(false);
            loginPage.SetActive(true);
        }
    }
    void HandleRandom1Photo(Sprite sprite)
    {
        if (sprite != null)
        {
            RandomImage1.sprite = sprite;
        }
    }
    void HandleRandom2Photo(Sprite sprite)
    {
        if (sprite != null)
        {
            RandomImage2.sprite = sprite;
        }
    }
    private IEnumerator LoadImageFromURL(string url, System.Action<Sprite> callback)
    {
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            callback?.Invoke(sprite);
        }
        else
        {
            Debug.LogError("Failed to load image: " + request.error);
        }
    }
    void HandleLoginResult(R r)
    {
        if (r != null)
        {
            if (r.IsSuccess())
            {
                Debug.Log("登录成功: " + r.message);
                PlayerPrefs.SetString("QWQToken", r.message);
                loginPage.SetActive(false);
                homePage.SetActive(true);
                hasLogined = true;
            }
            else
            {
                Debug.Log("登录失败: " + r.message);
                PlayerPrefs.DeleteKey("Username");
                PlayerPrefs.DeleteKey("Password");
                hasLogined = false;
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (UnityEngine.Input.GetKeyDown(KeyCode.F1))
        {
            ToggleCanvas();
        }
    }
    void ToggleCanvas()
    {
        if (page != null)
        {
            bool nextActive = !page.activeSelf;
            page.SetActive(nextActive);
            if (InputManager.Instance != null)
            {
                InputManager.Instance.disable = nextActive;
            }

            if (LightManager.Instance != null)
            {
                LightManager.Instance.LightToggle();
            }
            if (homePage != null)
            {
                if (!loadingPage.activeSelf)
                {
                    homePage.SetActive(hasLogined);
                }
            }
            if (loginPage != null)
            {
                loginPage.SetActive(!hasLogined);
            }
        }

        if (otherPanel != null)
        {
            bool isActive = otherPanel.activeSelf;
            otherPanel.SetActive(!isActive);

            StartCoroutine(ScanMusicCoroutine());

        }



    }

    private IEnumerator ScanMusicCoroutine()
    {
        Task scanMusicTask = SongStorage.ScanMusicAsync();
        // 等待任务完成
        while (!scanMusicTask.IsCompleted)
        {
            yield return null;
        }


        // 处理异常
        if (scanMusicTask.IsFaulted)
        {
            Debug.LogError("Error occurred during music scanning: " + scanMusicTask.Exception?.Message);
        }
        else
        {
            listManager.CoverListDisplayer.SetDirList(SongStorage.Songs);
            listManager.CoverListDisplayer.SetSongList();
            Debug.Log("Music scanning finished successfully.");
        }
    }
    private IEnumerator ApiGet(string kind, string method, string queryParams, int timeout, System.Action<R> callback)
    {
        // 获取本地存储的 token
        string token = PlayerPrefs.GetString("QWQToken", "-1");

        // 构建 URL，使用 queryParams 传递查询参数
        string url = queryParams != null && queryParams != "" ? $"{pre}{ip}/api/{kind}/{method}?{queryParams}" : $"{pre}{ip}/api/{kind}/{method}";
        Debug.Log(url);
        // 创建 UnityWebRequest 并设置请求头
        UnityWebRequest request = new(url, "GET")
        {
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("userToken", token);

        request.timeout = timeout / 1000;

        // 发送请求
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // 解析响应并处理
            R result = JsonConvert.DeserializeObject<R>(request.downloadHandler.text);
            if (result != null)
            {
                if (!result.IsSuccess())
                {
                    SetLoadingText(result.message);
                    loadingButton.SetActive(true);
                }
                Debug.Log(result.GetString());
                callback?.Invoke(result);
            }
        }
        else
        {
            // 处理错误响应
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                R r = new();
                if (request.error == "Request timeout")
                {
                    r.message = "服务器未响应，请刷新重试或重新提交！";
                }
                else if (request.error.Contains("network"))
                {
                    r.message = "网络错误，请刷新重试！";
                }
                else if (request.responseCode == 666)
                {
                    r.message = "Token失效，请重新登录！";
                    PlayerPrefs.DeleteKey("QWQToken");
                }
                SetLoadingText(r.message);
                loadingButton.SetActive(true);
                Debug.LogError($"{kind}/{method}发生错误：{r.message}");
                callback?.Invoke(r);
            }
        }
    }

    private IEnumerator ApiPost(string kind, string method, string data, int timeout, System.Action<R> callback)
    {
        // 获取本地存储的 token
        string token = PlayerPrefs.GetString("QWQToken", "-1");

        // 构建 URL
        string url = $"{pre}{ip}/api/{kind}/{method}";
        Debug.Log(url);
        // 创建 UnityWebRequest 并设置请求头
        UnityWebRequest request = new(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("userToken", token);

        request.timeout = timeout / 1000;

        // 发送请求
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            R result = JsonConvert.DeserializeObject<R>(request.downloadHandler.text);
            if (result != null)
            {
                callback?.Invoke(result);
            }
        }
        else
        {
            // 处理错误响应
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                R r = new();
                if (request.error == "Request timeout")
                {
                    r.message = "服务器未响应，请刷新重试或重新提交！";
                }
                else if (request.error.Contains("network"))
                {
                    r.message = "网络错误，请刷新重试！";
                }
                else if (request.responseCode == 666)
                {
                    r.message = "Token失效，请重新登录！";
                    PlayerPrefs.DeleteKey("QWQToken");
                }
                SetLoadingText(r.message);
                loadingButton.SetActive(true);
                Debug.LogError($"{kind}/{method}发生错误：{r.message}");
                callback?.Invoke(r);
            }
        }
    }
}
