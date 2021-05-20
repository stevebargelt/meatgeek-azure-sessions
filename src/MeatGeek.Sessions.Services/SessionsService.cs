using System;
using System.Linq;
using System.Threading.Tasks;
using MeatGeek.Sessions.Services.Models;
using MeatGeek.Sessions.Services.Models.Data;
using MeatGeek.Sessions.Services.Models.Response;
using MeatGeek.Sessions.Services.Models.Results;
using MeatGeek.Sessions.Services.Repositories;
using MeatGeek.Shared;
using MeatGeek.Shared.EventSchemas.Sessions;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Extensions;
using Microsoft.Extensions.Http;

// using ContentReactor.Shared.EventSchemas.Images;
// using ContentReactor.Shared.EventSchemas.Text;

namespace MeatGeek.Sessions.Services
{
    public interface ISessionsService
    {
        Task<string> AddSessionAsync(string title, string smokerId, DateTime startTime);
        Task<DeleteSessionResult> DeleteSessionAsync(string SessionId, string smokerId);
        Task<UpdateSessionResult> UpdateSessionAsync(string SessionId, string smokerId, string name, DateTime endTime);
        Task<SessionDetails> GetSessionAsync(string SessionId, string smokerId);
        Task<SessionSummaries> GetSessionsAsync(string smokerId);
        // Task ProcessAddItemEventAsync(EventGridEvent eventToProcess, string smokerId);
        // Task ProcessUpdateItemEventAsync(EventGridEvent eventToProcess, string smokerId);
        // Task ProcessDeleteItemEventAsync(EventGridEvent eventToProcess, string smokerId);
    }

    
    public class SessionsService : ISessionsService
    {
        //private ILogger<SessionsService> _log;
        protected ISessionsRepository SessionsRepository;
        protected IEventGridPublisherService EventGridPublisher;
        
        public SessionsService(ISessionsRepository sessionsRepository, IEventGridPublisherService eventGridPublisher)
        {
            SessionsRepository = sessionsRepository;
            EventGridPublisher = eventGridPublisher;
        }

        public async Task<string> AddSessionAsync(string title, string smokerId, DateTime startTime)
        {
            // create the document in Cosmos DB
            var SessionDocument = new SessionDocument
            {
                Title = title,
                SmokerId = smokerId,
                StartTime = startTime
            };
            var SessionId = await SessionsRepository.AddSessionAsync(SessionDocument);
            //_log.LogInformation("SessionId = " + SessionId);
            
            // post a SessionCreated event to Event Grid
            var eventData = new SessionCreatedEventData
            {
                Title = title
            };
            var subject = $"{smokerId}/{SessionId}";
            
            //_log.LogInformation("subject = " + subject);

            // await EventGridPublisher.PostEventGridEventAsync(EventTypes.Sessions.SessionCreated, subject, eventData);
            
            return SessionId;
        }

        public async Task<DeleteSessionResult> DeleteSessionAsync(string sessionId, string smokerId)
        {
            // delete the document from Cosmos DB
            var result = await SessionsRepository.DeleteSessionAsync(sessionId, smokerId);
            if (result == DeleteSessionResult.NotFound)
            {
                return DeleteSessionResult.NotFound;
            }

            // post a SessionDeleted event to Event Grid
            var subject = $"{smokerId}/{sessionId}";
            await EventGridPublisher.PostEventGridEventAsync(EventTypes.Sessions.SessionDeleted, subject, new SessionDeletedEventData());

            return DeleteSessionResult.Success;
        }

        public async Task<UpdateSessionResult> UpdateSessionAsync(string sessionId, string smokerId, string title, DateTime endTime)
        {
            // get the current version of the document from Cosmos DB
            var SessionDocument = await SessionsRepository.GetSessionAsync(sessionId, smokerId);
            if (SessionDocument == null)
            {
                return UpdateSessionResult.NotFound;
            }

            // update the document with the new name
            SessionDocument.Title = title;
            SessionDocument.EndTime = endTime;
            await SessionsRepository.UpdateSessionAsync(SessionDocument);

            // post a SessionNameUpdated event to Event Grid
            var eventData = new SessionTitleUpdatedEventData
            {
                Title = title,
                EndTime = endTime
            };
            var subject = $"{smokerId}/{sessionId}";
            await EventGridPublisher.PostEventGridEventAsync(EventTypes.Sessions.SessionTitleUpdated, subject, eventData);

            return UpdateSessionResult.Success;
        }

        public async Task<SessionDetails> GetSessionAsync(string sessionId, string smokerId)
        {
            var SessionDocument = await SessionsRepository.GetSessionAsync(sessionId, smokerId);
            if (SessionDocument == null)
            {
                return null;
            }

            return new SessionDetails
            {
                Id = SessionDocument.Id,
                Title = SessionDocument.Title,
                Description = SessionDocument.Description,
                StartTime = SessionDocument.StartTime,
                EndTime = SessionDocument.EndTime,
            };
        }

        
        public Task<SessionSummaries> GetSessionsAsync(string smokerId)
        {
            return SessionsRepository.GetSessionsAsync(smokerId);
        }

        // public async Task ProcessAddItemEventAsync(EventGridEvent eventToProcess, string smokerId)
        // {
        //     // process the item type
        //     var (item, SessionId, operationType) = ConvertEventGridEventToSessionItem(eventToProcess);
        //     if (operationType != OperationType.Add)
        //     {
        //         return;
        //     }

        //     // find the Session document
        //     var SessionDocument = await CategoriesRepository.GetSessionAsync(SessionId, smokerId);
        //     if (SessionDocument == null)
        //     {
        //         return;
        //     }
            
        //     // update the document with the new item
        //     // and if the item already exists in this Session, replace it
        //     var existingItem = SessionDocument.Items.SingleOrDefault(i => i.Id == item.Id);
        //     if (existingItem != null)
        //     {
        //         SessionDocument.Items.Remove(existingItem);
        //     }
        //     SessionDocument.Items.Add(item);
        //     await CategoriesRepository.UpdateSessionAsync(SessionDocument);

        //     // post a SessionItemsUpdated event to Event Grid
        //     var eventData = new SessionItemsUpdatedEventData();
        //     var subject = $"{smokerId}/{SessionDocument.Id}";
        //     await EventGridPublisher.PostEventGridEventAsync(EventTypes.Categories.SessionItemsUpdated, subject, eventData);
        // }

        // public async Task ProcessUpdateItemEventAsync(EventGridEvent eventToProcess, string smokerId)
        // {
        //     // process the item type
        //     var (updatedItem, _, operationType) = ConvertEventGridEventToSessionItem(eventToProcess);
        //     if (operationType != OperationType.Update)
        //     {
        //         return;
        //     }

        //     // find the Session document
        //     var SessionDocument = await CategoriesRepository.FindSessionWithItemAsync(updatedItem.Id, updatedItem.Type, smokerId);
        //     if (SessionDocument == null)
        //     {
        //         return;
        //     }

        //     // find the item in the document
        //     var existingItem = SessionDocument.Items.SingleOrDefault(i => i.Id == updatedItem.Id);
        //     if (existingItem == null)
        //     {
        //         return;
        //     }

        //     // update the item with the latest changes
        //     // (the only field that can change is Preview)
        //     existingItem.Preview = updatedItem.Preview;
        //     await CategoriesRepository.UpdateSessionAsync(SessionDocument);
            
        //     // post a SessionItemsUpdated event to Event Grid
        //     var eventData = new SessionItemsUpdatedEventData();
        //     var subject = $"{smokerId}/{SessionDocument.Id}";
        //     await EventGridPublisher.PostEventGridEventAsync(EventTypes.Categories.SessionItemsUpdated, subject, eventData);
        // }

        // public async Task ProcessDeleteItemEventAsync(EventGridEvent eventToProcess, string smokerId)
        // {
        //     // process the item type
        //     var (updatedItem, _, operationType) = ConvertEventGridEventToSessionItem(eventToProcess);
        //     if (operationType != OperationType.Delete)
        //     {
        //         return;
        //     }

        //     // find the Session document
        //     var SessionDocument = await CategoriesRepository.FindSessionWithItemAsync(updatedItem.Id, updatedItem.Type, smokerId);
        //     if (SessionDocument == null)
        //     {
        //         return;
        //     }
            
        //     // find the item in the document
        //     var itemToRemove = SessionDocument.Items.SingleOrDefault(i => i.Id == updatedItem.Id);
        //     if (itemToRemove == null)
        //     {
        //         return;
        //     }

        //     // remove the item from the document
        //     SessionDocument.Items.Remove(itemToRemove);
        //     await CategoriesRepository.UpdateSessionAsync(SessionDocument);

        //     // post a SessionItemsUpdated event to Event Grid
        //     var eventData = new SessionItemsUpdatedEventData();
        //     var subject = $"{smokerId}/{SessionDocument.Id}";
        //     await EventGridPublisher.PostEventGridEventAsync(EventTypes.Categories.SessionItemsUpdated, subject, eventData);
        // }
        
        // private (SessionItem SessionItem, string SessionId, OperationType operationType) ConvertEventGridEventToSessionItem(EventGridEvent eventToProcess)
        // {
        //     var item = new SessionItem
        //     {
        //         Id = eventToProcess.Subject.Split('/')[1] // we assume the subject has previously been checked for its format
        //     };

        //     string SessionId;
        //     OperationType operationType;
        //     switch (eventToProcess.EventType)
        //     {
        //         case EventTypes.Audio.AudioCreated:
        //             var audioCreatedEventData = (AudioCreatedEventData) eventToProcess.Data;
        //             item.Type = ItemType.Audio;
        //             item.Preview = audioCreatedEventData.TranscriptPreview;
        //             SessionId = audioCreatedEventData.Session;
        //             operationType = OperationType.Add;
        //             break;
                    
        //         case EventTypes.Images.ImageCreated:
        //             var imageCreatedEventData = (ImageCreatedEventData) eventToProcess.Data;
        //             item.Type = ItemType.Image;
        //             item.Preview = imageCreatedEventData.PreviewUri;
        //             SessionId = imageCreatedEventData.Session;
        //             operationType = OperationType.Add;
        //             break;

        //         case EventTypes.Text.TextCreated:
        //             var textCreatedEventData = (TextCreatedEventData) eventToProcess.Data;
        //             item.Type = ItemType.Text;
        //             item.Preview = textCreatedEventData.Preview;
        //             SessionId = textCreatedEventData.Session;
        //             operationType = OperationType.Add;
        //             break;

        //         case EventTypes.Audio.AudioTranscriptUpdated:
        //             var audioTranscriptUpdatedEventData = (AudioTranscriptUpdatedEventData) eventToProcess.Data;
        //             item.Type = ItemType.Audio;
        //             item.Preview = audioTranscriptUpdatedEventData.TranscriptPreview;
        //             SessionId = null;
        //             operationType = OperationType.Update;
        //             break;

        //         case EventTypes.Text.TextUpdated:
        //             var textUpdatedEventData = (TextUpdatedEventData) eventToProcess.Data;
        //             item.Type = ItemType.Text;
        //             item.Preview = textUpdatedEventData.Preview;
        //             SessionId = null;
        //             operationType = OperationType.Update;
        //             break;

        //         case EventTypes.Audio.AudioDeleted:
        //             item.Type = ItemType.Audio;
        //             SessionId = null;
        //             operationType = OperationType.Delete;
        //             break;

        //         case EventTypes.Images.ImageDeleted:
        //             item.Type = ItemType.Image;
        //             SessionId = null;
        //             operationType = OperationType.Delete;
        //             break;

        //         case EventTypes.Text.TextDeleted:
        //             item.Type = ItemType.Text;
        //             SessionId = null;
        //             operationType = OperationType.Delete;
        //             break;

        //         default:
        //             throw new ArgumentException($"Unexpected event type '{eventToProcess.EventType}' in {nameof(ProcessAddItemEventAsync)}");
        //     }

        //     if (operationType == OperationType.Add && string.IsNullOrEmpty(SessionId))
        //     {
        //         throw new InvalidOperationException("Session ID must be set for new items.");
        //     }
            
        //     return (item, SessionId, operationType);
        // }
    }
}
