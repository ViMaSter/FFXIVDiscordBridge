using System.Diagnostics;
using Sharlayan.Core;

namespace FFXIVByteParser.Test;

public class ChatLogParserTest
{
    private static byte[] ExtractResource(string filename)
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        using var resFilestream = assembly.GetManifestResourceStream(filename);
        Debug.Assert(resFilestream != null, "File not found: " + filename);
        var byteArray = new byte[resFilestream.Length];
        var readByteCount = resFilestream.Read(byteArray, 0, byteArray.Length);
        Debug.Assert(readByteCount != 0, "File empty: " + filename);
        return byteArray;
    }

    private const string CHANNEL_CODE = "0010";
    
    /// <summary>
    /// A specific set of byte streams generated by FFXIV can be converted to a human readable representation
    /// </summary>
    [Test]
    [TestCaseSource(nameof(GetChatsFromOtherCharacter))]
    public void UserReadableOutput(string fileName, byte[] actualInput, string expectedData)
    {
        // create logger
        var ffxivByteHandler = new FFXIVByteHandler(new NUnitLogger(), CHANNEL_CODE, "Sereth Milbana", "Cerberus", FFXIVByteHandler.CharacterNameDisplay.WITH_WORLD);
        Assert.That(ffxivByteHandler.TryFFXIVToDiscordFriendly(new ChatLogItem(){Bytes = actualInput, Line = System.Text.Encoding.UTF8.GetString(actualInput), Code = CHANNEL_CODE}, out var result), Is.True);
        CollectionAssert.AreEqual(expectedData, result);
    }
    
    /// <summary>
    /// Only messages from the expected channel are converted
    /// </summary>
    /// <remarks>
    /// Valid data is used, but only the channel code is changed
    /// </remarks>
    [Test]
    [TestCaseSource(nameof(GetChatsFromOtherCharacter))]
    public void IgnoreIrrelevantChannel(string fileName, byte[] actualInput, string expectedData)
    {
        var ffxivByteHandler = new FFXIVByteHandler(new NUnitLogger(), CHANNEL_CODE+"_", "Sereth Milbana", "Cerberus", FFXIVByteHandler.CharacterNameDisplay.WITH_WORLD);
        Assert.That(ffxivByteHandler.TryFFXIVToDiscordFriendly(new ChatLogItem(){Bytes = actualInput, Line = System.Text.Encoding.UTF8.GetString(actualInput), Code = CHANNEL_CODE}, out var result), Is.False);
        Assert.That(result, Is.Null);
    }
    
    /// <summary>
    /// The character used as bot is ignored
    /// </summary>
    [Test]
    [TestCaseSource(nameof(GetChatsFromOtherCharacter))]
    public void IgnoreMessagesFromCharacterThatActsAsBot(string fileName, byte[] actualInput, string expectedData)
    {
        var ffxivByteHandler = new FFXIVByteHandler(new NUnitLogger(), CHANNEL_CODE, "Sereth Milbana", "Cerberus", FFXIVByteHandler.CharacterNameDisplay.WITH_WORLD);
        Assert.That(ffxivByteHandler.TryFFXIVToDiscordFriendly(new ChatLogItem(){Bytes = actualInput, Line = "Sereth Milbana: " + System.Text.Encoding.UTF8.GetString(actualInput), Code = CHANNEL_CODE}, out var result), Is.False);
        Assert.That(result, Is.Null);
    }
    
    /// <summary>
    /// The character used as bot is ignored, unless "FORCEEXEC" is part of the message
    /// </summary>
    [Test]
    [TestCaseSource(nameof(GetChatsFromOwnCharacter))]
    public void ForceMessagesFromCharacterThatActsAsBot(string fileName, byte[] actualInput, string expectedData)
    {
        var ffxivByteHandler = new FFXIVByteHandler(new NUnitLogger(), CHANNEL_CODE, "Sereth Milbana", "Cerberus", FFXIVByteHandler.CharacterNameDisplay.WITH_WORLD);
        Assert.That(ffxivByteHandler.TryFFXIVToDiscordFriendly(new ChatLogItem(){Bytes = actualInput, Line = "Sereth Milbana: " + System.Text.Encoding.UTF8.GetString(actualInput) + "FORCEEXEC", Code = CHANNEL_CODE}, out var result), Is.True);
        CollectionAssert.AreEqual(expectedData, result);
    }
    
    /// <summary>
    /// The character used as bot is ignored
    /// </summary>
    [Test]
    [TestCaseSource(nameof(GetInvalidChats))]
    public void HandleInvalidChats(string fileName, byte[] actualInput, string expectedData)
    {
        var ffxivByteHandler = new FFXIVByteHandler(new NUnitLogger(), CHANNEL_CODE, "Sereth Milbana", "Cerberus", FFXIVByteHandler.CharacterNameDisplay.WITH_WORLD);
        Assert.That(ffxivByteHandler.TryFFXIVToDiscordFriendly(new ChatLogItem(){Bytes = actualInput, Line = System.Text.Encoding.UTF8.GetString(actualInput), Code = CHANNEL_CODE}, out var result), Is.False);
        Assert.That(result, Is.Null);
    }
    
    /// <summary>
    /// The character used as bot is ignored
    /// </summary>
    [Test]
    [TestCaseSource(nameof(GetEmptyChats))]
    public void HandleEmptyChats(string fileName, byte[] actualInput, string expectedData)
    {
        var ffxivByteHandler = new FFXIVByteHandler(new NUnitLogger(), CHANNEL_CODE, "Sereth Milbana", "Cerberus", FFXIVByteHandler.CharacterNameDisplay.WITH_WORLD);
        Assert.That(ffxivByteHandler.TryFFXIVToDiscordFriendly(new ChatLogItem(){Bytes = actualInput, Line = System.Text.Encoding.UTF8.GetString(actualInput), Code = CHANNEL_CODE}, out var result), Is.False);
        Assert.That(result, Is.Null);
    }

    public static IEnumerable<object[]> GetChatsFromOtherCharacter()
    {
        var currentAssembly = System.Reflection.Assembly.GetExecutingAssembly();
        return currentAssembly.GetManifestResourceNames()
            .Where(resourcePath => resourcePath.EndsWith(".binary"))
            .Where(resourcePath => resourcePath.Contains("FFXIVByteParser"))
            .Where(resourcePath => resourcePath.Contains("ChatsFromOtherCharacters"))
            .Select(pathToBinary => new object[] {
                pathToBinary.Replace(".binary", ""),
                ExtractResource(pathToBinary),
                System.Text.Encoding.UTF8.GetString(ExtractResource(pathToBinary.Replace(".binary", ".result")))
            });
    }

    public static IEnumerable<object[]> GetChatsFromOwnCharacter()
    {
        var currentAssembly = System.Reflection.Assembly.GetExecutingAssembly();
        return currentAssembly.GetManifestResourceNames()
            .Where(resourcePath => resourcePath.EndsWith(".binary"))
            .Where(resourcePath => resourcePath.Contains("FFXIVByteParser"))
            .Where(resourcePath => resourcePath.Contains("ChatsFromOwnCharacter"))
            .Select(pathToBinary => new object[] {
                pathToBinary.Replace(".binary", ""),
                ExtractResource(pathToBinary),
                System.Text.Encoding.UTF8.GetString(ExtractResource(pathToBinary.Replace(".binary", ".result")))
            });
    }

    public static IEnumerable<object[]> GetInvalidChats()
    {
        var currentAssembly = System.Reflection.Assembly.GetExecutingAssembly();
        return currentAssembly.GetManifestResourceNames()
            .Where(resourcePath => resourcePath.EndsWith(".binary"))
            .Where(resourcePath => resourcePath.Contains("FFXIVByteParser"))
            .Where(resourcePath => resourcePath.Contains("InvalidChats"))
            .Select(pathToBinary => new object[] {
                pathToBinary.Replace(".binary", ""),
                ExtractResource(pathToBinary),
                System.Text.Encoding.UTF8.GetString(ExtractResource(pathToBinary.Replace(".binary", ".result")))
            });
    }

    public static IEnumerable<object[]> GetEmptyChats()
    {
        var currentAssembly = System.Reflection.Assembly.GetExecutingAssembly();
        return currentAssembly.GetManifestResourceNames()
            .Where(resourcePath => resourcePath.EndsWith(".binary"))
            .Where(resourcePath => resourcePath.Contains("FFXIVByteParser"))
            .Where(resourcePath => resourcePath.Contains("EmptyChats"))
            .Select(pathToBinary => new object[] {
                pathToBinary.Replace(".binary", ""),
                ExtractResource(pathToBinary),
                System.Text.Encoding.UTF8.GetString(ExtractResource(pathToBinary.Replace(".binary", ".result")))
            });
    }
}