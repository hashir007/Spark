using Asp.Versioning;
using SparkApp.APIModel.Event;
using SparkApp.APIModel.Organizer;
using SparkApp.APIModel.User;
using SparkApp.APIModel.Venue;
using SparkApp.Security.Policy;
using SparkService.Models;
using SparkService.Services;
using SparkService.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using System;
using System.Diagnostics.Metrics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography.Xml;

namespace SparkApp.Controllers
{
    [ApiController]
    [Authorize]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/events")]
    public class EventController : Controller
    {
        private readonly ILogger<EventController> _logger;
        private readonly IAuthorizationService _authorizationService;
        private readonly VenuesService _venuesService;
        private readonly EventsService _eventsService;
        private readonly EventOrganizersService _eventOrganizersService;


        public EventController(ILogger<EventController> logger, IAuthorizationService authorizationService, VenuesService venuesService, EventsService eventsService, EventOrganizersService eventOrganizersService)
            => (_logger, _authorizationService, _venuesService, _eventsService, _eventOrganizersService) = (logger, authorizationService, venuesService, eventsService, eventOrganizersService);


        [MapToApiVersion(1)]
        [HttpGet("venues/{id}")]
        public async Task<ActionResult<ResponseModel<VenuesViewModel>>> GetVenue([FromRoute][BindRequired] string id)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var venue = await _venuesService.GetAsync(id);

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, await _venuesService.GetAsync(id), Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = venue;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("venues")]
        public async Task<ActionResult<ResponseModel<VenuesViewModel>>> GetVenuesAll()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var venues = await _venuesService.GetAsync();

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new Venues() { created_by = string.Empty }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            List<VenuesViewModel> list = new List<VenuesViewModel>();

            foreach (var venue in venues)
            {
                list.Add(new VenuesViewModel().ToVenuesViewModel(venue));
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = list;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("venues/{page:int}/{pageSize:int}/{term?}")]
        public async Task<ActionResult<ResponseModel<List<VenuesViewModel>>>> GetVenues([FromRoute][BindRequired] int page, [FromRoute][BindRequired] int pageSize, [Optional][FromRoute] string? term)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var venues = _venuesService.Search(string.IsNullOrEmpty(term) ? string.Empty : term);

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new Venues() { created_by = string.Empty }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            List<VenuesViewModel> list = new List<VenuesViewModel>();

            foreach (var venue in venues)
            {
                list.Add(new VenuesViewModel().ToVenuesViewModel(venue));
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = list.Count(),
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                page = page,
                pageSize = pageSize
            };

            return Ok(responseModel);
        }



        [MapToApiVersion(1)]
        [HttpPut("venues/{id}")]
        public async Task<ActionResult<ResponseModel<VenuesViewModel>>> UpdateVenues([FromRoute][BindRequired] string id, [FromBody] VenueUpdateRequest model)
        {
            ResponseModel<VenuesViewModel> responseModel = new ResponseModel<VenuesViewModel>();

            Venues? venue = await _venuesService.GetAsync(id!);

            if (venue == null)
            {
                _logger.LogError($"SparkApp.Controllers.EventController.UpdateVenues Error = Venue not found. For Id = {id}");
                throw new Exception($"Venue not found. For Id = {id}");
            }

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (userId is null)
            {
                _logger.LogError($"SparkApp.Controllers.EventController.UpdateVenues Error = userId = {userId} not found.");
                throw new Exception("userId not found");
            }

            venue!.name = model.name;
            venue!.street = model.street;
            venue!.street2 = model.street2;
            venue!.city = model.city;
            venue!.state = model.state;
            venue!.zip = model.zip;
            venue!.country = model.country;
            venue!.manager_name = model.manager_name;
            venue!.manager_phone = model.manager_phone;
            venue!.manager_email = model.manager_email;
            venue!.modified_by = userId!;
            venue!.updated_at = DateTime.UtcNow;

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, venue, Operations.Update);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            await _venuesService.UpdateAsync(id, venue);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new VenuesViewModel().ToVenuesViewModel(venue!);

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("{id}")]
        public async Task<ActionResult<ResponseModel<EventsViewModel>>> GetEvent([FromRoute][BindRequired] string id)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var @event = await _eventsService.GetAsync(id);

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, await _eventsService.GetAsync(id), Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var viewModel = new EventsViewModel().ToEventsViewModel(@event);
            viewModel.Venues = await _venuesService.GetAsync(@event.venueId) ?? new Venues();
            viewModel.Organizers = await _eventOrganizersService.GetAsync(@event.organizerId) ?? new EventOrganizers();

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = viewModel;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost()]
        public async Task<ActionResult<ResponseModel<EventsViewModel>>> CreateEvent([FromBody] EventCreateRequest model)
        {
            ResponseModel<EventsViewModel> responseModel = new ResponseModel<EventsViewModel>();

            Events newEvent = new Events();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (userId is null)
            {
                _logger.LogError($"SparkApp.Controllers.EventController.CreateEvent Error = userId = {userId} not found.");
                throw new Exception("userId not found");
            }

            newEvent.name = model.name;
            newEvent.description = model.description;
            newEvent.status = model.status;
            newEvent.start_date = Convert.ToDateTime(model.start_date).ToUniversalTime();
            newEvent.attendees = model.attendees;
            newEvent.capacity = model.capacity;
            newEvent.created_by = userId!;
            newEvent.created_at = DateTime.UtcNow;
            newEvent.organizerId = model.organizerId;
            newEvent.venueId = model.venueId;
            newEvent.type = model.type;

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, newEvent, Operations.Create);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            await _eventsService.AddAsync(newEvent);

            var @event = await _eventsService.GetAsync(newEvent.Id!);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new EventsViewModel().ToEventsViewModel(@event!);

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPut("{id}")]
        public async Task<ActionResult<ResponseModel<EventsViewModel>>> UpdateEvent([FromRoute][BindRequired] string id, [FromBody] EventUpdateRequest model)
        {
            ResponseModel<EventsViewModel> responseModel = new ResponseModel<EventsViewModel>();

            Events? @event = await _eventsService.GetAsync(id!);

            if (@event == null)
            {
                _logger.LogError($"SparkApp.Controllers.EventController.UpdateEvent Error = Venue not found. For Id = {id}");
                throw new Exception($"Venue not found. For Id = {id}");
            }

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (username is null)
            {
                _logger.LogError($"SparkApp.Controllers.EventController.UpdateEvent Error = username = {username} not found.");
                throw new Exception("username not found");
            }

            @event.name = model.name;
            @event.description = model.description;
            @event.status = model.status;
            @event.start_date = Convert.ToDateTime(model.start_date).ToUniversalTime();
            @event.attendees = model.attendees;
            @event.capacity = model.capacity;
            @event.modified_by = userId!;
            @event.updated_at = DateTime.UtcNow;
            @event.organizerId = model.organizerId;
            @event.venueId = model.venueId;
            @event.type = model.type;

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, @event, Operations.Update);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            await _eventsService.UpdateAsync(id, @event);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new EventsViewModel().ToEventsViewModel(@event!);

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("{page:int}/{pageSize:int}/{term?}")]
        public async Task<ActionResult<ResponseModel<List<EventsViewModel>>>> GetEvents([FromRoute][BindRequired] int page, [FromRoute][BindRequired] int pageSize, [Optional][FromRoute] string? term)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var @events = _eventsService.Search(string.IsNullOrEmpty(term) ? string.Empty : term);

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new Events() { created_by = string.Empty }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            List<EventsViewModel> list = new List<EventsViewModel>();

            foreach (var venue in @events)
            {
                var viewModel = new EventsViewModel().ToEventsViewModel(venue);
                viewModel.Venues = await _venuesService.GetAsync(viewModel.venueId) ?? new Venues();
                viewModel.Organizers = await _eventOrganizersService.GetAsync(viewModel.organizerId) ?? new EventOrganizers();
                list.Add(viewModel);
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = list.Count(),
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                page = page,
                pageSize = pageSize
            };

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("organizer")]
        public async Task<ActionResult<ResponseModel<EventOrganizersViewModel>>> GetEventOrganizersAll()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var organizers = await _eventOrganizersService.GetAsync();

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new Venues() { created_by = string.Empty }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            List<EventOrganizersViewModel> list = new List<EventOrganizersViewModel>();

            foreach (var organizer in organizers)
            {
                list.Add(new EventOrganizersViewModel().ToEventOrganizersViewModel(organizer));
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = list;

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("organizer/{page:int}/{pageSize:int}/{term?}")]
        public async Task<ActionResult<ResponseModel<List<VenuesViewModel>>>> GetEventOrganizers([FromRoute][BindRequired] int page, [FromRoute][BindRequired] int pageSize, [Optional][FromRoute] string? term)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var organizers = _eventOrganizersService.Search(string.IsNullOrEmpty(term) ? string.Empty : term);

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, new Venues() { created_by = string.Empty }, Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            List<EventOrganizersViewModel> list = new List<EventOrganizersViewModel>();

            foreach (var organizer in organizers)
            {
                list.Add(new EventOrganizersViewModel().ToEventOrganizersViewModel(organizer));
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new
            {
                Total = list.Count(),
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                page = page,
                pageSize = pageSize
            };

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpGet("organizer/{id}")]
        public async Task<ActionResult<ResponseModel<EventOrganizersViewModel>>> GetEventOrganizer([FromRoute][BindRequired] string id)
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var organizer = await _eventOrganizersService.GetAsync(id);

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, await _eventOrganizersService.GetAsync(id), Operations.Read);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new EventOrganizersViewModel().ToEventOrganizersViewModel(organizer!);

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPost("organizer")]
        public async Task<ActionResult<ResponseModel<EventOrganizersViewModel>>> CreateEventOrganizer([FromBody] EventOrganizerCreateRequest model)
        {
            ResponseModel<EventOrganizersViewModel> responseModel = new ResponseModel<EventOrganizersViewModel>();

            EventOrganizers newOrganizer = new EventOrganizers();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (username is null)
            {
                _logger.LogError($"SparkApp.Controllers.EventController.CreateEventOrganizer Error = username = {username} not found.");
                throw new Exception("username not found");
            }

            newOrganizer.name = model.name;
            newOrganizer.email = model.email;
            newOrganizer.type = model.type;
            newOrganizer.created_by = userId!;
            newOrganizer.created_at = DateTime.UtcNow;

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, newOrganizer, Operations.Create);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            await _eventOrganizersService.AddAsync(newOrganizer);

            var @event = await _eventOrganizersService.GetAsync(newOrganizer.Id!);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new EventOrganizersViewModel().ToEventOrganizersViewModel(newOrganizer);

            return Ok(responseModel);
        }


        [MapToApiVersion(1)]
        [HttpPut("organizer/{id}")]
        public async Task<ActionResult<ResponseModel<EventOrganizersViewModel>>> UpdateEventOrganizer([FromRoute][BindRequired] string id, [FromBody] EventOrganizerUpdateRequest model)
        {
            ResponseModel<EventOrganizersViewModel> responseModel = new ResponseModel<EventOrganizersViewModel>();

            EventOrganizers? organizer = await _eventOrganizersService.GetAsync(id!);

            if (organizer == null)
            {
                _logger.LogError($"SparkApp.Controllers.EventController.UpdateEventOrganizer Error = organizer not found. For Id = {id}");
                throw new Exception($"Venue not found. For Id = {id}");
            }

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            if (username is null)
            {
                _logger.LogError($"SparkApp.Controllers.EventController.UpdateEventOrganizer Error = username = {username} not found.");
                throw new Exception("username not found");
            }

            organizer.name = model.name;
            organizer.email = model.email;
            organizer.type = model.type;
            organizer.modified_by = userId!;
            organizer.updated_at = DateTime.UtcNow;

            var isAuthorized = await _authorizationService.AuthorizeAsync(this.User, organizer, Operations.Update);

            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            await _eventOrganizersService.UpdateAsync(id, organizer);

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = new EventOrganizersViewModel().ToEventOrganizersViewModel(organizer!);

            return Ok(responseModel);
        }



    }
}
