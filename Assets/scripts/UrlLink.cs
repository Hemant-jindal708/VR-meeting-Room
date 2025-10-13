using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.VisualScripting;
using UnityEngine;

public class UrlLink : MonoBehaviour
{
    string url = "https://models.readyplayer.me/68e7dc009f7e763dce218914.glb";
    void Awake()
    {
        loadurl();
    }
    public void SetUrl(string url)
    {
        this.url = url;
        FileStream fileStream = new FileStream(Path.Combine(Application.persistentDataPath, "data.li"), FileMode.Open);
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        binaryFormatter.Serialize(fileStream, url);
        fileStream.Close();
    }
    public string getUrl()
    {
        return url;
    }
    void loadurl()
    {
        string filepath = Path.Combine(Application.persistentDataPath, "data.li");
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        if (File.Exists(filepath))
        {
            FileStream stream = new FileStream(filepath, FileMode.Open);
            string Url = (string)binaryFormatter.Deserialize(stream);
            stream.Close();
            this.url = Url;
        }
        else
        {
            FileStream stream = new FileStream(filepath, FileMode.Create);
            binaryFormatter.Serialize(stream, url);
            stream.Close();
        }
    }
}
