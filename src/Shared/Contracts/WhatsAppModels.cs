using System.Text.Json.Serialization;

namespace Sigma.Shared.Contracts;

// WhatsApp Business API models for incoming webhooks
// Reference: https://developers.facebook.com/docs/whatsapp/cloud-api/webhooks

public class WhatsAppWebhookEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("changes")]
    public List<WhatsAppWebhookChange>? Changes { get; set; }
}

public class WhatsAppWebhookChange
{
    [JsonPropertyName("value")]
    public WhatsAppWebhookValue? Value { get; set; }

    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;
}

public class WhatsAppWebhookValue
{
    [JsonPropertyName("messaging_product")]
    public string MessagingProduct { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public WhatsAppMetadata? Metadata { get; set; }

    [JsonPropertyName("contacts")]
    public List<WhatsAppContact>? Contacts { get; set; }

    [JsonPropertyName("messages")]
    public List<WhatsAppIncomingMessage>? Messages { get; set; }

    [JsonPropertyName("statuses")]
    public List<WhatsAppStatus>? Statuses { get; set; }
}

public class WhatsAppMetadata
{
    [JsonPropertyName("display_phone_number")]
    public string DisplayPhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("phone_number_id")]
    public string PhoneNumberId { get; set; } = string.Empty;
}

public class WhatsAppContact
{
    [JsonPropertyName("profile")]
    public WhatsAppProfile? Profile { get; set; }

    [JsonPropertyName("wa_id")]
    public string WaId { get; set; } = string.Empty;
}

public class WhatsAppProfile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class WhatsAppIncomingMessage
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public WhatsAppTextMessage? Text { get; set; }

    [JsonPropertyName("image")]
    public WhatsAppMediaMessage? Image { get; set; }

    [JsonPropertyName("document")]
    public WhatsAppMediaMessage? Document { get; set; }

    [JsonPropertyName("video")]
    public WhatsAppMediaMessage? Video { get; set; }

    [JsonPropertyName("context")]
    public WhatsAppContext? Context { get; set; }

    [JsonPropertyName("interactive")]
    public WhatsAppInteractive? Interactive { get; set; }
}

public class WhatsAppTextMessage
{
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

public class WhatsAppMediaMessage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("mime_type")]
    public string? MimeType { get; set; }

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }

    [JsonPropertyName("caption")]
    public string? Caption { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }
}

public class WhatsAppContext
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class WhatsAppInteractive
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("button_reply")]
    public WhatsAppButtonReply? ButtonReply { get; set; }

    [JsonPropertyName("list_reply")]
    public WhatsAppListReply? ListReply { get; set; }
}

public class WhatsAppButtonReply
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}

public class WhatsAppListReply
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class WhatsAppStatus
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("recipient_id")]
    public string RecipientId { get; set; } = string.Empty;
}