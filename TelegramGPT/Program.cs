using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Threading;
using Telegram.Bots.Http;
using TelegramGPT.Data;
using System.Net.Http.Json;

class Program
{
    private static readonly HttpClient httpClient = new();
    private static List<TelegramGPT.Data.Message> messages = new();
    private const string GptApiKey = "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";
    private const string GptEndPoint = "https://api.openai.com/v1/chat/completions";        
    const string TelegramApiKey = "6568617162:AAEBC5SAxodwN5u8X8DyYxb_c3vT-lhkgE8";
    private static void Main(string[] args)
    {
        ITelegramBotClient telegramBotClient = new TelegramBotClient(TelegramApiKey);

        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {GptApiKey}");

        var tokenSource = new CancellationTokenSource();

        ReceiverOptions receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        telegramBotClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: tokenSource.Token
            );

        Console.ReadLine();
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken token)
    {
        if (update.Message is not { } message)
        {
            return;
        }
        if (message.Text is not { } messageText)
        {
            return;
        }

        var chatId = message.Chat.Id;

        var responseTextFromGpt = await GetGptResponse(messageText);

        Telegram.Bot.Types.Message sentMessage = await client.SendTextMessageAsync(
        chatId: chatId,
        text: responseTextFromGpt,
        cancellationToken: token);
    }


    private static async Task HandlePollingErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        await Task.CompletedTask;
    }
    private static async Task<string> GetGptResponse(string messageText)
    {

        var message = new TelegramGPT.Data.Message() { Role = "user", Content = messageText};
        messages.Add(message);

        var requestData = new Request()
        {
            ModelId = "gpt-3.5-turbo",
            Messages = messages
        };

        using var response = await httpClient.PostAsJsonAsync(GptEndPoint, requestData);

        if (!response.IsSuccessStatusCode)
        {
            return ($"{(int)response.StatusCode} {response.StatusCode}");
        }

        ResponseData? responseData = await response.Content.ReadFromJsonAsync<ResponseData>();
        
        var choices = responseData?.Choices ?? new List<Choice>();
        if (choices.Count == 0)
        {
            return ("No choices were returned by the API");
        }

        var choice = choices[0];

        var responseMessage = choice.Message;

        messages.Add(responseMessage);

        var responseText = responseMessage.Content.Trim();

        return responseText;
    }

}