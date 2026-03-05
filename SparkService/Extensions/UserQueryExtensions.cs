using SparkService.Models;
using SparkService.ViewModels;
using Microsoft.AspNetCore.Http;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static SparkService.Services.UsersService;

namespace SparkService.Extensions
{
    public static class UserQueryExtensions
    {

        public static IQueryable<UserViewModel> ApplyRoleFilter(this IQueryable<UserViewModel> query, string role = "User")
        {
            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(user => user.roles.Any(x => x.name == role));
            }

            return query;
        }

        public static IQueryable<UserViewModel> ApplySorting(this IQueryable<UserViewModel> query, UserSortOption userSortOption)
        {
            return userSortOption switch
            {

                UserSortOption.created_at => query.OrderByDescending(user => user.created_at),
                UserSortOption.name => query.OrderByDescending(user => user.profile.first_name),
                _ => query.OrderByDescending(user => user.created_at)
            };
        }

        public static IQueryable<UserViewModel> ApplyPagination(this IQueryable<UserViewModel> query, int pageNumber, int pageSize)
        {
            return query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }

        public static IQueryable<UserViewModelV2> ToUsersV2(this IQueryable<UserViewModel> query)
        {
            return query.Select((user) => new UserViewModelV2
            {
                id = user.id,
                is_active = user.is_active,
                language = user.language,
                timezone = user.timezone,
                username = user.username,
                last_login = user.last_login,
                profile = new ProfileViewModelV2
                {
                    aboutYourselfInYourOwnWords = user.profile.aboutYourselfInYourOwnWords,
                    bio = user.profile.bio,
                    bodyType = user.profile.bodyType,
                    city = user.profile.city,
                    country = user.profile.country,
                    date_of_birth = user.profile.date_of_birth,
                    age = user.profile.age,
                    describeThePersonYouAreLookingFor = user.profile.describeThePersonYouAreLookingFor,
                    gender = user.profile.gender.ToString(),
                    iam = user.profile.iam.ToString(),
                    martialStatus = user.profile.martialStatus,
                    photo = new FileViewModel
                    {
                        id = user.profile.photo.id,
                        orignalName = user.profile.photo.orignalName,
                        type = user.profile.photo.type,
                        name = user.profile.photo.name,
                        size = user.profile.photo.size,
                        original = user.profile.photo.original,
                        d480x320 = user.profile.photo.d480x320,
                        d300x300 = user.profile.photo.d300x300,
                        d100x100 = user.profile.photo.d100x100,
                        d16x16 = user.profile.photo.d16x16,
                        d32x32 = user.profile.photo.d32x32
                    },
                    seeking = user.profile.seeking.ToString(),
                    educationLevel = user.profile.educationLevel,
                    relationshipGoals = user.profile.relationshipGoals,
                    race = user.profile.race,
                    state = user.profile.state,
                    zip_code = user.profile.zip_code,
                    height = user.profile.height,
                    profileHeadline = user.profile.profileHeadline,
                    annualIncome = user.profile.annualIncome
                },
                Subscription = new SubscriptionV3ViewModel
                {
                    Plan = user.Subscription.Plan,
                    status = user.Subscription.status,
                    end_date = user.Subscription.end_date,
                    start_date = user.Subscription.start_date,
                }
            });
        }

        public static IQueryable<UserViewModelV3> ToUsersV3(this IQueryable<UserViewModel> query)
        {
            return query.Select((user) => new UserViewModelV3
            {
                id = user.id,
                is_active = user.is_active,
                language = user.language,
                timezone = user.timezone,
                username = user.username,
                last_login = user.last_login,
                profile = new ProfileViewModelV3
                {
                    city = user.profile.city,
                    country = user.profile.country,
                    age = user.profile.age,
                    gender = user.profile.gender.ToString(),
                    photo = new FileViewModel
                    {
                        id = user.profile.photo.id,
                        orignalName = user.profile.photo.orignalName,
                        type = user.profile.photo.type,
                        name = user.profile.photo.name,
                        size = user.profile.photo.size,
                        original = user.profile.photo.original,
                        d480x320 = user.profile.photo.d480x320,
                        d300x300 = user.profile.photo.d300x300,
                        d100x100 = user.profile.photo.d100x100,
                        d16x16 = user.profile.photo.d16x16,
                        d32x32 = user.profile.photo.d32x32
                    },
                    state = user.profile.state,
                    zip_code = user.profile.zip_code,
                },
                Subscription = new SubscriptionV3ViewModel
                {
                    Plan = user.Subscription.Plan,
                    status = user.Subscription.status,
                    end_date = user.Subscription.end_date,
                    start_date = user.Subscription.start_date,
                }
            });
        }

        public static IQueryable<UserViewModelV4> ToUsersV4(this IQueryable<UserViewModel> query)
        {
            return query.Select((user) => new UserViewModelV4
            {
                id = user.id,
                is_active = user.is_active,
                language = user.language,
                timezone = user.timezone,
                username = user.username,
                last_login = user.last_login,
                profile = new ProfileViewModelV4
                {
                    photo = new FileViewModel
                    {
                        id = user.profile.photo.id,
                        orignalName = user.profile.photo.orignalName,
                        type = user.profile.photo.type,
                        name = user.profile.photo.name,
                        size = user.profile.photo.size,
                        original = user.profile.photo.original,
                        d480x320 = user.profile.photo.d480x320,
                        d300x300 = user.profile.photo.d300x300,
                        d100x100 = user.profile.photo.d100x100,
                        d16x16 = user.profile.photo.d16x16,
                        d32x32 = user.profile.photo.d32x32
                    }
                },
                Subscription = new SubscriptionV3ViewModel
                {
                    Plan = user.Subscription.Plan,
                    status = user.Subscription.status,
                    end_date = user.Subscription.end_date,
                    start_date = user.Subscription.start_date,
                }
            });
        }

    }
}
