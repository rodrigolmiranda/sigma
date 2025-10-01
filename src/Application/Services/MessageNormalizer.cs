using Sigma.Shared.Contracts;
using Sigma.Shared.Enums;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Sigma.Application.Services;

/// <summary>
/// Transforms platform-specific message payloads into canonical MessageEvent schema
/// </summary>
public interface IMessageNormalizer
{
    MessageEvent NormalizeTelegramMessage(TelegramUpdate update, Guid tenantId, Guid workspaceId);
    MessageEvent NormalizeWhatsAppMessage(WhatsAppIncomingMessage message, WhatsAppMetadata metadata, Guid tenantId, Guid workspaceId, string tenantSalt);
}

public class MessageNormalizer : IMessageNormalizer
{
    public MessageEvent NormalizeTelegramMessage(TelegramUpdate update, Guid tenantId, Guid workspaceId)
    {
        var telegramMessage = update.Message ?? update.EditedMessage ?? update.ChannelPost ?? update.EditedChannelPost;

        if (telegramMessage == null)
        {
            throw new ArgumentException("Update contains no message", nameof(update));
        }

        var messageEvent = new MessageEvent
        {
            Platform = Platform.Telegram,
            PlatformMessageId = telegramMessage.MessageId.ToString(),
            PlatformChannelId = telegramMessage.Chat?.Id.ToString(),
            WorkspaceId = workspaceId,
            TenantId = tenantId,
            TimestampUtc = DateTimeOffset.FromUnixTimeSeconds(telegramMessage.Date).UtcDateTime,
            EditedUtc = telegramMessage.EditDate.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(telegramMessage.EditDate.Value).UtcDateTime
                : null,
            Text = telegramMessage.Text,
            Type = DetermineMessageType(telegramMessage),
            Raw = JsonSerializer.SerializeToDocument(update)
        };

        // Set sender info
        if (telegramMessage.From != null)
        {
            messageEvent.Sender = new MessageSenderInfo
            {
                PlatformUserId = telegramMessage.From.Id.ToString(),
                DisplayName = GetTelegramDisplayName(telegramMessage.From),
                IsBot = telegramMessage.From.IsBot
            };
        }
        else if (telegramMessage.SenderChat != null)
        {
            // Anonymous channel admins
            messageEvent.Sender = new MessageSenderInfo
            {
                PlatformUserId = telegramMessage.SenderChat.Id.ToString(),
                DisplayName = telegramMessage.SenderChat.Title ?? "Anonymous Channel",
                IsBot = false
            };
        }

        // Extract reply reference
        if (telegramMessage.ReplyToMessage != null)
        {
            messageEvent.ReplyToPlatformMessageId = telegramMessage.ReplyToMessage.MessageId.ToString();
        }

        // Extract rich fragments (entities)
        if (telegramMessage.Entities != null && !string.IsNullOrEmpty(telegramMessage.Text))
        {
            messageEvent.RichFragments = ExtractTelegramEntities(telegramMessage.Text, telegramMessage.Entities);
        }

        // Extract media
        if (telegramMessage.Photo != null && telegramMessage.Photo.Any())
        {
            var largestPhoto = telegramMessage.Photo.OrderByDescending(p => p.Width * p.Height).First();
            messageEvent.Media.Add(new MessageMedia
            {
                Url = $"telegram://file/{largestPhoto.FileId}",
                MimeType = "image/jpeg",
                Size = largestPhoto.FileSize
            });
        }

        if (telegramMessage.Document != null)
        {
            messageEvent.Media.Add(new MessageMedia
            {
                Url = $"telegram://file/{telegramMessage.Document.FileId}",
                MimeType = telegramMessage.Document.MimeType ?? "application/octet-stream",
                Size = telegramMessage.Document.FileSize,
                FileName = telegramMessage.Document.FileName
            });
        }

        if (telegramMessage.Video != null)
        {
            messageEvent.Media.Add(new MessageMedia
            {
                Url = $"telegram://file/{telegramMessage.Video.FileId}",
                MimeType = telegramMessage.Video.MimeType ?? "video/mp4",
                Size = telegramMessage.Video.FileSize
            });
        }

        return messageEvent;
    }

    public MessageEvent NormalizeWhatsAppMessage(WhatsAppIncomingMessage message, WhatsAppMetadata metadata,
        Guid tenantId, Guid workspaceId, string tenantSalt)
    {
        var messageEvent = new MessageEvent
        {
            Platform = Platform.WhatsApp,
            PlatformMessageId = message.Id,
            PlatformChannelId = $"wa:{HashPhoneNumber(message.From, tenantSalt)}", // Pseudo-channel per user
            WorkspaceId = workspaceId,
            TenantId = tenantId,
            TimestampUtc = DateTimeOffset.FromUnixTimeSeconds(long.Parse(message.Timestamp)).UtcDateTime,
            Type = DetermineWhatsAppMessageType(message),
            Raw = JsonSerializer.SerializeToDocument(message)
        };

        // Set sender info (hashed phone for privacy)
        messageEvent.Sender = new MessageSenderInfo
        {
            PlatformUserId = HashPhoneNumber(message.From, tenantSalt),
            DisplayName = GetWhatsAppDisplayName(message.From), // Show last 4 digits
            IsBot = false
        };

        // Extract text content
        if (message.Text != null)
        {
            messageEvent.Text = message.Text.Body;
        }
        else if (message.Image?.Caption != null)
        {
            messageEvent.Text = message.Image.Caption;
        }
        else if (message.Document?.Caption != null)
        {
            messageEvent.Text = message.Document.Caption;
        }
        else if (message.Video?.Caption != null)
        {
            messageEvent.Text = message.Video.Caption;
        }

        // Extract reply reference
        if (message.Context != null)
        {
            messageEvent.ReplyToPlatformMessageId = message.Context.Id;
        }

        // Extract media
        if (message.Image != null)
        {
            messageEvent.Media.Add(new MessageMedia
            {
                Url = $"whatsapp://media/{message.Image.Id}",
                MimeType = message.Image.MimeType ?? "image/jpeg"
            });
        }

        if (message.Document != null)
        {
            messageEvent.Media.Add(new MessageMedia
            {
                Url = $"whatsapp://media/{message.Document.Id}",
                MimeType = message.Document.MimeType ?? "application/octet-stream",
                FileName = message.Document.Filename
            });
        }

        if (message.Video != null)
        {
            messageEvent.Media.Add(new MessageMedia
            {
                Url = $"whatsapp://media/{message.Video.Id}",
                MimeType = message.Video.MimeType ?? "video/mp4"
            });
        }

        return messageEvent;
    }

    private static MessageEventType DetermineMessageType(TelegramMessage message)
    {
        if (message.Photo != null && message.Photo.Any())
            return MessageEventType.Image;

        if (message.Document != null || message.Video != null)
            return MessageEventType.File;

        if (message.NewChatMembers != null || message.LeftChatMember != null)
            return MessageEventType.System;

        if (!string.IsNullOrEmpty(message.Text))
            return MessageEventType.Text;

        return MessageEventType.Unknown;
    }

    private static MessageEventType DetermineWhatsAppMessageType(WhatsAppIncomingMessage message)
    {
        return message.Type switch
        {
            "text" => MessageEventType.Text,
            "image" => MessageEventType.Image,
            "document" or "video" or "audio" => MessageEventType.File,
            "interactive" => MessageEventType.Text, // Button/list replies treated as text
            _ => MessageEventType.Unknown
        };
    }

    private static string GetTelegramDisplayName(TelegramUser user)
    {
        if (!string.IsNullOrEmpty(user.Username))
            return $"@{user.Username}";

        var fullName = user.FirstName;
        if (!string.IsNullOrEmpty(user.LastName))
            fullName += $" {user.LastName}";

        return fullName;
    }

    private static string GetWhatsAppDisplayName(string phoneNumber)
    {
        // Show only last 4 digits for privacy (rest is hashed)
        if (phoneNumber.Length >= 4)
            return $"***{phoneNumber[^4..]}";

        return "***";
    }

    private static string HashPhoneNumber(string phoneNumber, string tenantSalt)
    {
        // SHA256 hash with tenant-specific salt for privacy
        var inputBytes = Encoding.UTF8.GetBytes(phoneNumber + tenantSalt);
        var hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToBase64String(hashBytes);
    }

    private static List<MessageFragment> ExtractTelegramEntities(string text, List<TelegramMessageEntity> entities)
    {
        var fragments = new List<MessageFragment>();

        foreach (var entity in entities)
        {
            var fragment = new MessageFragment
            {
                StartIndex = entity.Offset,
                EndIndex = entity.Offset + entity.Length,
                Value = text.Substring(entity.Offset, entity.Length)
            };

            fragment.Type = entity.Type switch
            {
                "url" or "text_link" => FragmentType.Link,
                "mention" or "text_mention" => FragmentType.Mention,
                "hashtag" => FragmentType.Hashtag,
                "code" or "pre" => FragmentType.Code,
                _ => FragmentType.Link // Default
            };

            if (entity.Type == "text_link" && !string.IsNullOrEmpty(entity.Url))
            {
                fragment.Url = entity.Url;
            }
            else if (entity.Type == "url")
            {
                fragment.Url = fragment.Value;
            }

            fragments.Add(fragment);
        }

        return fragments;
    }
}