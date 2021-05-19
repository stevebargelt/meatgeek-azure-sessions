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

namespace MeatGeek.Sessions.Services
{
    public interface ISessionsServiceDI
    {
        Task<string> AddSessionAsync(string title, string smokerId, DateTime startTime, ISessionsRepository sessionsRepository, IEventGridPublisherService eventGridPublisher);
        Task<DeleteSessionResult> DeleteSessionAsync(string SessionId, string smokerId, ISessionsRepository sessionsRepository, IEventGridPublisherService eventGridPublisher);
        Task<UpdateSessionResult> UpdateSessionAsync(string SessionId, string smokerId, string name, DateTime endTime, ISessionsRepository sessionsRepository, IEventGridPublisherService eventGridPublisher);
        Task<SessionDetails> GetSessionAsync(string SessionId, string smokerId, ISessionsRepository sessionsRepository, IEventGridPublisherService eventGridPublisher);
        Task<SessionSummaries> GetSessionsAsync(string smokerId, ISessionsRepository sessionsRepository, IEventGridPublisherService eventGridPublisher);
    }

    
    public class SessionsServiceDI : ISessionsServiceDI
    {
        private ILogger<SessionsServiceDI> _log;

        public SessionsServiceDI(ILogger<SessionsServiceDI> logger)
        {
            _log = logger;
        }

        public async Task<string> AddSessionAsync(string title, string smokerId, DateTime startTime, ISessionsRepository sessionsRepository, IEventGridPublisherService eventGridPublisher)
        {
            // create the document in Cosmos DB
            var SessionDocument = new SessionDocument
            {
                Title = title,
                SmokerId = smokerId,
                StartTime = startTime
            };
            var SessionId = await sessionsRepository.AddSessionAsync(SessionDocument);
            _log.LogInformation("SessionId = " + SessionId);
            
            // post a SessionCreated event to Event Grid
            var eventData = new SessionCreatedEventData
            {
                Title = title
            };
            var subject = $"{smokerId}/{SessionId}";
            
            _log.LogInformation("subject = " + subject);

            await eventGridPublisher.PostEventGridEventAsync(EventTypes.Sessions.SessionCreated, subject, eventData);
            
            return SessionId;
        }

        public async Task<DeleteSessionResult> DeleteSessionAsync(string sessionId, string smokerId, ISessionsRepository sessionsRepository, IEventGridPublisherService eventGridPublisher)
        {
            // delete the document from Cosmos DB
            var result = await sessionsRepository.DeleteSessionAsync(sessionId, smokerId);
            if (result == DeleteSessionResult.NotFound)
            {
                return DeleteSessionResult.NotFound;
            }

            // post a SessionDeleted event to Event Grid
            var subject = $"{smokerId}/{sessionId}";
            await eventGridPublisher.PostEventGridEventAsync(EventTypes.Sessions.SessionDeleted, subject, new SessionDeletedEventData());

            return DeleteSessionResult.Success;
        }

        public async Task<UpdateSessionResult> UpdateSessionAsync(string sessionId, string smokerId, string title, DateTime endTime, ISessionsRepository sessionsRepository, IEventGridPublisherService eventGridPublisher)
        {
            // get the current version of the document from Cosmos DB
            var SessionDocument = await sessionsRepository.GetSessionAsync(sessionId, smokerId);
            if (SessionDocument == null)
            {
                return UpdateSessionResult.NotFound;
            }

            // update the document with the new name
            SessionDocument.Title = title;
            SessionDocument.EndTime = endTime;
            await sessionsRepository.UpdateSessionAsync(SessionDocument);

            // post a SessionNameUpdated event to Event Grid
            var eventData = new SessionTitleUpdatedEventData
            {
                Title = title,
                EndTime = endTime
            };
            var subject = $"{smokerId}/{sessionId}";
            await eventGridPublisher.PostEventGridEventAsync(EventTypes.Sessions.SessionTitleUpdated, subject, eventData);

            return UpdateSessionResult.Success;
        }

        public async Task<SessionDetails> GetSessionAsync(string sessionId, string smokerId, ISessionsRepository sessionsRepository, IEventGridPublisherService eventGridPublisher)
        {
            var SessionDocument = await sessionsRepository.GetSessionAsync(sessionId, smokerId);
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

        
        public Task<SessionSummaries> GetSessionsAsync(string smokerId, ISessionsRepository sessionsRepository, IEventGridPublisherService eventGridPublisher)
        {
            return sessionsRepository.GetSessionsAsync(smokerId);
        }

    }
}
