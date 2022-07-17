using System.Net;
using HtmlAgilityPack;

namespace InstaSaveFarsiBot.Utility;

public class IGDownloader
{
    public async Task<string> GetLink(string url)
    {
        string mainUrl = MakeLink(url);

        // getting html data for provided url
        WebRequest req = WebRequest.Create(mainUrl);
        HttpWebResponse res = (HttpWebResponse)req.GetResponse();
        Stream stream = res.GetResponseStream();
        StreamReader sr = new StreamReader(stream);
        string html = sr.ReadToEnd();

        return GetUrl(html, mainUrl);
    }

    private string MakeLink(string url)
    {
        string[] splittedUrl = url.Split("/");
        string mainLink = $"{splittedUrl[0]}//{splittedUrl[2]}/{splittedUrl[3]}/{splittedUrl[4]}/embed";
        return mainLink;
    }

    private string GetUrl(string html, string url)
    {
        // checking for post type
        if (html.Contains("video_url"))
        {
            return GetVideoLink(html);
        }
        else
        {
            return GetPictureUrl(url);
        }
    }

    private string GetVideoLink(string html)
    {
        // find start and end of video_url object
        int start = html.IndexOf("video_url");
        int end = start + 1000;
        string finalLink = "{" + '"' + html[start..end];
        finalLink = finalLink.Substring(0, finalLink.IndexOf(",")) + ", " + '"' + "type" + '"' + ":" + '"' + "video" + '"' +  "}";

        return finalLink;
    }

    private string GetPictureUrl(string url)
    {
        // get html from url
        var webGet = new HtmlWeb();
        var doc = webGet.Load(url);

        // select src of img tag with class EmbeddedMediaImage from html
        var node = doc.DocumentNode.SelectSingleNode("//img[@class='EmbeddedMediaImage']");
        var baseLink = node.GetAttributeValue("src", "notFound");

        string finalLink = MakeFinalLink(baseLink);
        finalLink = "{" + '"' + "photo_url" + '"' + ":" + '"' + finalLink + '"' + "," + '"' + "type" + '"' + ":" + '"' + "photo" + '"' + "}" ;

        return finalLink;
    }


    private string MakeFinalLink(string link)
    {
        var splittedLink = link.Split("&amp;");
        string finalLink = "";

        // replace all &amp; with &
        foreach (string s in splittedLink)
        {
            finalLink += s + "&";
        }

        return finalLink;
    }

}
