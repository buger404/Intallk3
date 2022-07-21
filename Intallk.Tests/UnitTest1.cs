using Intallk.Models;
using JiebaNet.Analyser;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using WordCloudSharp;
using Xunit;

namespace Intallk.Tests;

public class UnitTest1 : BasicTest
{
    internal UnitTest1(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    /**public internal UnitTest1(WebApplicationFactory<Program> factory) : base(factory)
{
}**/
    [Fact]
    public void WordCloudAnalyze()
    {
        //string text = File.ReadAllText("D:\\word.txt");
        //TfidfExtractor tfidfExtractor = new TfidfExtractor();
        //List<WordWeightPair> key = tfidfExtractor.ExtractTagsWithWeight(text).ToList();
        Assert.True(false);
    }
    [Fact]
    public void WordCloud()
    {
        string text = File.ReadAllText("D:\\word.txt");
        TfidfExtractor tfidfExtractor = new TfidfExtractor();
        List<WordWeightPair> key = tfidfExtractor.ExtractTagsWithWeight(text, 100, null).ToList();
        WordCloud wc = new WordCloud(1280, 1440, fontname: "HarmonyOS Sans SC Medium", allowVerical: true);
        List<int> freqs = new List<int>();
        foreach (WordWeightPair wp in key) freqs.Add((int)(wp.Weight * 1000));
        Image wi = wc.Draw(key.Select(it => it.Word).ToList(), freqs);
        string file = "D:\\wordcloud.png";
        wi.Save(file, ImageFormat.Png);
        Assert.True(File.Exists("D:\\wordcloud.png"));
    }
}
