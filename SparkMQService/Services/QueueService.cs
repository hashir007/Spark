using SparkService.Models;
using SparkService.Services;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Newtonsoft.Json;
using RabbitMQ.Client.Core.DependencyInjection;
using RabbitMQ.Client.Core.DependencyInjection.MessageHandlers;
using RabbitMQ.Client.Core.DependencyInjection.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkMQService.Services
{
    public class QueueService : IMessageHandler
    {
        private readonly ILogger<QueueService> _logger;
        private readonly CompatibilityScoresService _compatibilityScoresService;
        private readonly UsersService _usersService;
        private readonly ProfilesService _profilesService;
        private readonly TraitsService _traitsService;
        private readonly InterestsService _interestsService;
        public QueueService(ILogger<QueueService> logger, CompatibilityScoresService compatibilityScoresService, UsersService usersService, TraitsService traitsService, InterestsService interestsService, ProfilesService profilesService)
        {
            _logger = logger;
            _compatibilityScoresService = compatibilityScoresService;
            _usersService = usersService;
            _traitsService = traitsService;
            _interestsService = interestsService;
            _profilesService = profilesService;
        }

        public void Handle(MessageHandlingContext context, string matchingRoute)
        {
            try
            {
                _logger.LogInformation($"Handling message {context.Message.GetMessage()} by routing key {matchingRoute}");

                var QueueBody = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(context.Message.GetMessage());

                var QueueResult = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(QueueBody);

                string userWhoUpdatedProfile = QueueResult.userId;

                if (!string.IsNullOrEmpty(userWhoUpdatedProfile))
                {
                    var users = _usersService.GetAllUsersIds();

                    users = users.Where(x => x != userWhoUpdatedProfile).ToList();

                    // FETCH USER DETAILS
                    var userDetailsWhoUpdatedProfile = _usersService.GetDetailed(userWhoUpdatedProfile);

                    _logger.LogInformation($"Calculating compatibility score for user userId = {userWhoUpdatedProfile} BEGIN");

                    foreach (var user in users)
                    {
                        int score = 0;

                        // FETCH OTHER USER DETAILS

                        var otherUserDetails = _usersService.GetDetailed(user);


                        _logger.LogInformation($"Calculating compatibilty score with comparision for other user userId = {user}");


                        // CALCULATE AGE DIFFERENCE

                        int ageDiff = Math.Abs(Convert.ToInt32(otherUserDetails!.profile.age) - Convert.ToInt32(userDetailsWhoUpdatedProfile!.profile.age));


                        _logger.LogInformation($"Calculating age-difference = {ageDiff}");

                        // CALCULATE AGE SCORE START

                        if (ageDiff <= 5)
                        {
                            score = score + 20;
                            _logger.LogInformation($"Calculating age difference score = {20}");
                        }
                        else if (ageDiff <= 10)
                        {
                            score = score + 10;
                            _logger.LogInformation($"Calculating age difference score = {10}");
                        }
                        else if (ageDiff <= 15)
                        {
                            score = score + 5;
                            _logger.LogInformation($"Calculating age difference score = {5}");
                        }
                        else
                        {
                            score = score + 0;
                            _logger.LogInformation($"Calculating age difference score = {0}");
                        }


                        // CALCULATE AGE SCORE END

                        // CALCULATE LOCATION SCORE START

                        userDetailsWhoUpdatedProfile.profile.country = userDetailsWhoUpdatedProfile.profile.country is null ? string.Empty : userDetailsWhoUpdatedProfile.profile.country;
                        userDetailsWhoUpdatedProfile.profile.state = userDetailsWhoUpdatedProfile.profile.state is null ? string.Empty : userDetailsWhoUpdatedProfile.profile.state;
                        userDetailsWhoUpdatedProfile.profile.city = userDetailsWhoUpdatedProfile.profile.city is null ? string.Empty : userDetailsWhoUpdatedProfile.profile.city;
                        userDetailsWhoUpdatedProfile.profile.zip_code = userDetailsWhoUpdatedProfile.profile.zip_code is null ? string.Empty : userDetailsWhoUpdatedProfile.profile.zip_code;

                        otherUserDetails.profile.country = otherUserDetails.profile.country is null ? string.Empty : otherUserDetails.profile.country;
                        otherUserDetails.profile.state = otherUserDetails.profile.state is null ? string.Empty : otherUserDetails.profile.state;
                        otherUserDetails.profile.city = otherUserDetails.profile.city is null ? string.Empty : otherUserDetails.profile.city;
                        otherUserDetails.profile.zip_code = otherUserDetails.profile.zip_code is null ? string.Empty : otherUserDetails.profile.zip_code;

                        if (userDetailsWhoUpdatedProfile.profile.country.ToLower() == otherUserDetails.profile.country.ToLower()
                            && userDetailsWhoUpdatedProfile.profile.state.ToLower() == otherUserDetails.profile.state.ToLower()
                            && userDetailsWhoUpdatedProfile.profile.city.ToLower() == otherUserDetails.profile.city.ToLower()
                            && userDetailsWhoUpdatedProfile.profile.zip_code.ToLower() == otherUserDetails.profile.zip_code.ToLower())
                        {
                            score = score + 15;
                            _logger.LogInformation($"Calculating location score = {15}");
                        }
                        else
                        {
                            score = score + 0;
                            _logger.LogInformation($"Calculating location score = {0}");
                        }

                        // CALCULATE LOCATION SCORE END

                        // CALCULATE EDUCATION SCORE START

                        userDetailsWhoUpdatedProfile.profile.educationLevel = userDetailsWhoUpdatedProfile.profile.educationLevel is null ? string.Empty : userDetailsWhoUpdatedProfile.profile.educationLevel;
                        otherUserDetails.profile.educationLevel = otherUserDetails.profile.educationLevel is null ? string.Empty : otherUserDetails.profile.educationLevel;

                        if (userDetailsWhoUpdatedProfile.profile.educationLevel.ToLower() == otherUserDetails.profile.educationLevel.ToLower())
                        {
                            score = score + 10;
                            _logger.LogInformation($"Calculating location score = {10}");
                        }
                        else
                        {
                            score = score + 0;
                            _logger.LogInformation($"Calculating location score = {0}");
                        }

                        // CALCUALTE EDUCATION SCORE END

                        // CALCULATE RELATIONSHIP GOALS SCORE START

                        userDetailsWhoUpdatedProfile.profile.relationshipGoals = userDetailsWhoUpdatedProfile.profile.relationshipGoals is null ? string.Empty : userDetailsWhoUpdatedProfile.profile.relationshipGoals;
                        otherUserDetails.profile.relationshipGoals = otherUserDetails.profile.relationshipGoals is null ? string.Empty : otherUserDetails.profile.relationshipGoals;

                        if (userDetailsWhoUpdatedProfile.profile.relationshipGoals.ToLower() == otherUserDetails.profile.relationshipGoals.ToLower())
                        {
                            score = score + 15;
                            _logger.LogInformation($"Calculating location score = {15}");
                        }
                        else
                        {
                            score = score + 0;
                            _logger.LogInformation($"Calculating location score = {0}");
                        }

                        // CALCULATE RELATIONSHIP GOALS END

                        // CALCULATE GENDER SCORE START

                        userDetailsWhoUpdatedProfile.profile.gender = userDetailsWhoUpdatedProfile.profile.gender is null ? string.Empty : userDetailsWhoUpdatedProfile.profile.gender;
                        otherUserDetails.profile.gender = otherUserDetails.profile.gender is null ? string.Empty : otherUserDetails.profile.gender;

                        if ((userDetailsWhoUpdatedProfile.profile.gender == "Male" && otherUserDetails.profile.gender == "Female")
                            || (otherUserDetails.profile.gender == "Female" && userDetailsWhoUpdatedProfile.profile.gender == "Male")
                            )
                        {
                            score = score + 10;
                            _logger.LogInformation($"Calculating gender score = {10}");
                        }
                        else if (userDetailsWhoUpdatedProfile.profile.gender == "Non-binary"
                            && otherUserDetails.profile.gender == "Non-binary")
                        {
                            score = score + 8;
                            _logger.LogInformation($"Calculating gender score = {8}");
                        }
                        else if (userDetailsWhoUpdatedProfile.profile.gender == "Male" || otherUserDetails.profile.gender == "Female"
                            && otherUserDetails.profile.gender == "Non-binary")
                        {
                            score = score + 5;
                            _logger.LogInformation($"Calculating gender score = {5}");
                        }
                        else if (userDetailsWhoUpdatedProfile.profile.gender == "Non-binary" && (otherUserDetails.profile.gender == "Male"
                           || otherUserDetails.profile.gender == "Female"))
                        {
                            score = score + 5;
                            _logger.LogInformation($"Calculating gender score = {5}");
                        }
                        else
                        {
                            score = score + 0;
                            _logger.LogInformation($"Calculating gender score = {0}");
                        }


                        // CALCULATE GENDER SCORE END

                        // CALCULATE TRAITS SCORE START

                        var userTraits = _traitsService.GetUserTraits(userWhoUpdatedProfile);

                        var otherUserTraits = _traitsService.GetUserTraits(user);

                        var commonTraits = userTraits.Select(x => x.trait_id).ToList().Intersect(otherUserTraits.Select(x => x.trait_id).ToList());

                        if (commonTraits.Any())
                        {
                            score = score + (commonTraits.Count() * 10);
                            _logger.LogInformation($"Calculating traits score = {(commonTraits.Count() * 10)}");
                        }
                        else
                        {
                            score = score + 0;
                            _logger.LogInformation($"Calculating traits score = {0}");
                        }


                        // CALCULATE TRAITS SCORE END


                        // CALCULATE INTERESTS SCORE START

                        var userInterests = _interestsService.GetUserInterests(userWhoUpdatedProfile);

                        var otherUserInterests = _interestsService.GetUserInterests(user);

                        var commonInterest = userInterests.Select(x => x.interest_description).ToList().Intersect(otherUserInterests.Select(x => x.interest_description).ToList());

                        if (commonInterest.Any())
                        {
                            score = score + (commonInterest.Count() * 5);
                            _logger.LogInformation($"Calculating interests score = {(commonInterest.Count() * 5)}");
                        }
                        else
                        {
                            score = score + 0;
                            _logger.LogInformation($"Calculating interests score = {0}");
                        }

                        // CALCULATE INTERESTS SCORE END

                        _logger.LogInformation($"Total score = {score}");

                        // ADD OR UPDATE SCORE FOR OTHER USER START

                        var compatibilityScore = Task.Run(async () => await _compatibilityScoresService.GetAsync(userWhoUpdatedProfile, user)).Result;

                        if (compatibilityScore is null)
                        {
                            var compatibilityScoreNew = new CompatibilityScores();
                            compatibilityScoreNew.score = score;
                            compatibilityScoreNew.user_id = userWhoUpdatedProfile;
                            compatibilityScoreNew.other_user_id = user;

                            Task.Run(async () => await _compatibilityScoresService.CreateAsync(compatibilityScoreNew));
                        }
                        else
                        {
                            compatibilityScore.score = score;
                            Task.Run(async () => await _compatibilityScoresService.CreateAsync(compatibilityScore));
                        }

                        // ADD OR UPDATE SCORE FOR OTHER USER END

                    }

                    _logger.LogInformation($"Calculating compatibility score for user userId = {userWhoUpdatedProfile} END");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }


    }
}
