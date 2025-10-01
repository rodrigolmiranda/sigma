using System.Text.Json.Serialization;

namespace Sigma.Shared.Contracts;

// Telegram Bot API models for incoming updates
// Reference: https://core.telegram.org/bots/api#update

public class TelegramUpdate
{
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    [JsonPropertyName("message")]
    public TelegramMessage? Message { get; set; }

    [JsonPropertyName("edited_message")]
    public TelegramMessage? EditedMessage { get; set; }

    [JsonPropertyName("channel_post")]
    public TelegramMessage? ChannelPost { get; set; }

    [JsonPropertyName("edited_channel_post")]
    public TelegramMessage? EditedChannelPost { get; set; }
}

public class TelegramMessage
{
    [JsonPropertyName("message_id")]
    public long MessageId { get; set; }

    [JsonPropertyName("from")]
    public TelegramUser? From { get; set; }

    [JsonPropertyName("sender_chat")]
    public TelegramChat? SenderChat { get; set; }

    [JsonPropertyName("date")]
    public long Date { get; set; }

    [JsonPropertyName("chat")]
    public TelegramChat? Chat { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("entities")]
    public List<TelegramMessageEntity>? Entities { get; set; }

    [JsonPropertyName("photo")]
    public List<TelegramPhotoSize>? Photo { get; set; }

    [JsonPropertyName("document")]
    public TelegramDocument? Document { get; set; }

    [JsonPropertyName("video")]
    public TelegramVideo? Video { get; set; }

    [JsonPropertyName("reply_to_message")]
    public TelegramMessage? ReplyToMessage { get; set; }

    [JsonPropertyName("edit_date")]
    public long? EditDate { get; set; }

    [JsonPropertyName("new_chat_members")]
    public List<TelegramUser>? NewChatMembers { get; set; }

    [JsonPropertyName("left_chat_member")]
    public TelegramUser? LeftChatMember { get; set; }
}

public class TelegramUser
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("is_bot")]
    public bool IsBot { get; set; }

    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }
}

public class TelegramChat
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }
}

public class TelegramMessageEntity
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("user")]
    public TelegramUser? User { get; set; }
}

public class TelegramPhotoSize
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } = string.Empty;

    [JsonPropertyName("file_unique_id")]
    public string FileUniqueId { get; set; } = string.Empty;

    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}

public class TelegramDocument
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } = string.Empty;

    [JsonPropertyName("file_unique_id")]
    public string FileUniqueId { get; set; } = string.Empty;

    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }

    [JsonPropertyName("mime_type")]
    public string? MimeType { get; set; }

    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; }
}

public class TelegramVideo
{
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } = string.Empty;

    [JsonPropertyName("file_unique_id")]
    public string FileUniqueId { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("mime_type")]
    public string? MimeType { get; set; }

    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; }
}