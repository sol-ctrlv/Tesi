using UnityEngine;

public class OpenBrowserUrl : MonoBehaviour
{

    public void OpenWebsite(string url = "https://forms.gle/gKMYZMCe2FbHUtXg9")
    {
        Application.OpenURL(url);
    }

}
