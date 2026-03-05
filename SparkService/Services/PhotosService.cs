using SparkService.Models;
using SparkService.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkService.Services
{
    public class PhotosService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMongoCollection<Photos> _photosCollection;
        private readonly IMongoCollection<Models.File> _filesCollection;
        private readonly IMongoCollection<ViewsPhoto> _viewsPhotoCollection;
        private readonly IMongoCollection<LikesDisLikesPhoto> _likesDisLikesPhotoCollection;
        private readonly UsersService _usersService;
        private readonly SubscriptionService _subscriptionService;

        public PhotosService(IOptions<SparkDatabaseSettings> happySugarDaddyDatabaseSettings, UsersService usersService, SubscriptionService subscriptionService, IHttpContextAccessor httpContextAccessor)
        {
            var mongoClient = new MongoClient(
               happySugarDaddyDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                happySugarDaddyDatabaseSettings.Value.DatabaseName);

            _photosCollection = mongoDatabase.GetCollection<Photos>(
                 happySugarDaddyDatabaseSettings.Value.PhotosCollectionName);

            _filesCollection = mongoDatabase.GetCollection<Models.File>(
              happySugarDaddyDatabaseSettings.Value.FileCollectionName);

            _viewsPhotoCollection = mongoDatabase.GetCollection<ViewsPhoto>(
             happySugarDaddyDatabaseSettings.Value.ViewsPhotoCollectionName);

            _likesDisLikesPhotoCollection = mongoDatabase.GetCollection<Models.LikesDisLikesPhoto>(
             happySugarDaddyDatabaseSettings.Value.LikesDisLikesPhotoCollectionName);

            _usersService = usersService;

            _subscriptionService = subscriptionService;

            _httpContextAccessor = httpContextAccessor;
        }

        public async Task CreateAsync(Photos profile) =>
             await _photosCollection.InsertOneAsync(profile);

        public async Task UpdateAsync(string Id, Photos updatedProfile) =>
            await _photosCollection.ReplaceOneAsync(x => x.Id == Id, updatedProfile);

        public (List<PhotosViewModel>, int) GetUserPhotos(string id, string term, int page, int pageSize)
        {
            var query = (from p in _photosCollection.AsQueryable()
                         join f in _filesCollection.AsQueryable()
                         on p.fileId equals f.Id
                         where p.userId == id
                         select new PhotosViewModel
                         {
                             created_at = p.created_at,
                             updated_at = p.updated_at,
                             userId = p.userId,
                             fileId = p.fileId,
                             passCode = p.passCode,
                             Id = p.Id,
                             is_adult = p.is_adult,
                             is_featured = p.is_featured,
                             is_private = p.is_private,
                             is_members_only = p.is_members_only,
                             file = new FileViewModel
                             {
                                 id = f.Id,
                                 orignalName = f.originalName,
                                 type = f.type,
                                 name = f.name,
                                 size = f.size,
                                 original = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_original)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_original))),
                                 d480x320 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_480x320)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_480x320))),
                                 d300x300 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_300x300)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_300x300))),
                                 d100x100 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_100x100)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_100x100))),
                                 d16x16 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_16x16)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_16x16))),
                                 d32x32 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_32x32)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_32x32)))
                             },
                         });

            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(x => x.Id! == (term).Trim() || x.file.name! == (term).Trim());
            }

            var photos = query.OrderByDescending(x => x.created_at).Skip(((page - 1) * pageSize)).Take(pageSize).ToList();

            photos.ForEach(z => z.user = _usersService.GetDetailedV3(z.userId!)!);
            photos.ForEach(z => z.file.size = Math.Round(z.file.size, 2, MidpointRounding.AwayFromZero));

            return (photos, query.Count());
        }

        public (List<PhotosViewModel>, int) GetMemberPhotos(string id, int page, int pageSize)
        {
            var query = (from p in _photosCollection.AsQueryable()
                         join f in _filesCollection.AsQueryable()
                         on p.fileId equals f.Id
                         where p.userId == id
                         select new PhotosViewModel
                         {
                             created_at = p.created_at,
                             updated_at = p.updated_at,
                             userId = p.userId,
                             fileId = p.fileId,
                             Id = p.Id,
                             is_adult = p.is_adult,
                             is_featured = p.is_featured,
                             is_private = p.is_private,
                             is_members_only = p.is_members_only,
                             file = new FileViewModel
                             {
                                 id = f.Id,
                                 orignalName = f.originalName,
                                 type = f.type,
                                 name = f.name,
                                 size = f.size,
                                 original = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_original)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_original))),
                                 d480x320 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_480x320)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_480x320))),
                                 d300x300 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_300x300)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_300x300))),
                                 d100x100 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_100x100)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_100x100))),
                                 d16x16 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_16x16)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_16x16))),
                                 d32x32 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_32x32)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_32x32)))
                             },
                         });

            var photos = query.OrderByDescending(x => x.created_at).Skip(((page - 1) * pageSize)).Take(pageSize).ToList();

            photos.ForEach(z => z.user = _usersService.GetDetailedV3(z.userId!)!);

            var subscription = _subscriptionService.GetSubscription(id);

            foreach (var photo in photos)
            {
                if (photo.is_private)
                {
                    photo.file = new FileViewModel();
                }
                else if (photo.is_members_only)
                {
                    if (subscription!.Plan.type != SubscriptionPlanTypes.free.ToString())
                    {
                        photo.file = new FileViewModel();
                    }
                }
            }

            return (photos, query.Count());
        }

        public PhotosViewModel? GetDetailed(string id)
        {
            var query = (from p in _photosCollection.AsQueryable()
                         join f in _filesCollection.AsQueryable()
                         on p.fileId equals f.Id
                         where p.Id == id
                         select new PhotosViewModel
                         {
                             created_at = p.created_at,
                             updated_at = p.updated_at,
                             userId = p.userId,
                             fileId = p.fileId,
                             passCode = p.passCode,
                             Id = p.Id,
                             is_adult = p.is_adult,
                             is_featured = p.is_featured,
                             is_private = p.is_private,
                             is_members_only = p.is_members_only,
                             file = new FileViewModel
                             {
                                 id = f.Id,
                                 orignalName = f.originalName,
                                 type = f.type,
                                 name = f.name,
                                 size = f.size,
                                 original = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_original)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_original))),
                                 d480x320 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_480x320)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_480x320))),
                                 d300x300 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_300x300)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_300x300))),
                                 d100x100 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_100x100)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_100x100))),
                                 d16x16 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_16x16)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_16x16))),
                                 d32x32 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_32x32)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_32x32)))
                             },
                         });
            var photo = query.FirstOrDefault();
            if (photo is not null)
            {
                photo.user = _usersService.GetDetailedV3(photo.userId!)!;
                photo.file.size = Math.Round(photo.file.size, 2, MidpointRounding.AwayFromZero);
            }

            return photo;
        }
        public async Task<Photos> GetAsync(string id)
        {
            return await _photosCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task DeletePhoto(string id) => await _photosCollection.DeleteOneAsync(x => x.Id == id);

        public long GetUserPhotosCount(string id)
        {
            return _photosCollection.Find(x => x.userId == id).CountDocuments();
        }

        public double GetUserGallerySize(string userId)
        {
            var photos = (from p in _photosCollection.AsQueryable()
                          join f in _filesCollection.AsQueryable()
                          on p.fileId equals f.Id
                          where p.userId == userId
                          select new PhotosViewModel
                          {
                              Id = p.Id,
                              file = new FileViewModel
                              {
                                  size = f.size
                              },
                          });

            double sizeConsumed = 0;

            foreach (var photo in photos)
            {
                sizeConsumed = sizeConsumed + photo.file.size;
            }

            return Math.Round(sizeConsumed, 2, MidpointRounding.AwayFromZero);
        }

        public async Task RemoveFeaturedAttributeFromPhotos(string userId)
        {
            var photosWithFeaturedAttribute = await _photosCollection.Find(x => x.userId == userId && x.is_featured == true).ToListAsync();

            foreach (var item in photosWithFeaturedAttribute)
            {
                item.is_featured = false;
                await _photosCollection.ReplaceOneAsync(x => x.Id == item.Id, item);
            }
        }

        public PhotosViewModel? GetUserFeaturedPhoto(string userId)
        {
            var query = (from p in _photosCollection.AsQueryable()
                         join f in _filesCollection.AsQueryable()
                         on p.fileId equals f.Id
                         where p.userId == userId && p.is_featured == true
                         select new PhotosViewModel
                         {
                             created_at = p.created_at,
                             updated_at = p.updated_at,
                             userId = p.userId,
                             fileId = p.fileId,
                             Id = p.Id,
                             is_adult = p.is_adult,
                             is_featured = p.is_featured,
                             is_private = p.is_private,
                             is_members_only = p.is_members_only,
                             file = new FileViewModel
                             {
                                 id = f.Id,
                                 orignalName = f.originalName,
                                 type = f.type,
                                 name = f.name,
                                 size = f.size,
                                 original = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_original)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_original))),
                                 d480x320 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_480x320)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_480x320))),
                                 d300x300 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_300x300)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_300x300))),
                                 d100x100 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_100x100)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_100x100))),
                                 d16x16 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_16x16)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_16x16))),
                                 d32x32 = (_httpContextAccessor.HttpContext == null ? ("/Store/" + (f.query_32x32)) : (_httpContextAccessor.HttpContext!.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value.ToString() + "/Store/" + (f.query_32x32)))
                             },
                         });
            var photo = query.FirstOrDefault();
            if (photo is not null)
            {
                photo.user = _usersService.GetDetailedV3(photo.userId!)!;
                photo.file.size = Math.Round(photo.file.size, 2, MidpointRounding.AwayFromZero);
            }

            return photo;
        }

        public LikesDislikesPhotoViewModel GetLikesDislikesPhotoSummary(string userId, string photoId)
        {
            var likes = _likesDisLikesPhotoCollection.AsQueryable()
                              .Where(x => x.photo_id == photoId && x.isLikes).Count();

            var disLikes = _likesDisLikesPhotoCollection.AsQueryable()
                              .Where(x => x.photo_id == photoId && !x.isLikes).Count();

            var likesDisLikesPhoto = _likesDisLikesPhotoCollection.Find(x => x.photo_id == photoId && x.user_id == userId).FirstOrDefault();

            return new LikesDislikesPhotoViewModel
            {
                totalDislikesCount = disLikes,
                totalLikesCount = likes,
                LikesDisLikesPhoto = likesDisLikesPhoto
            };
        }

        public async Task CreateLikesDislikesPhotoAsync(LikesDisLikesPhoto photo) =>
            await _likesDisLikesPhotoCollection.InsertOneAsync(photo);

        public LikesDisLikesPhoto GetLikesDisLikesPhoto(string userId, string photoId)
        {
            return _likesDisLikesPhotoCollection.Find(x => x.photo_id == photoId && x.user_id == userId).FirstOrDefault();
        }

        public async Task UpdateLikesDisLikesPhotoAsync(string Id, LikesDisLikesPhoto updatedLikesDisLikesPhoto) =>
           await _likesDisLikesPhotoCollection.ReplaceOneAsync(x => x.Id == Id, updatedLikesDisLikesPhoto);

        public async Task CreateViewsPhotoAsync(ViewsPhoto view)
        {
            await _viewsPhotoCollection.InsertOneAsync(view);
        }

        public ViewsPhotoViewModel GetViewsForPhoto(string photoId)
        {
            var views = _viewsPhotoCollection.AsQueryable().Where(x => x.photoId == photoId).Count();

            return new ViewsPhotoViewModel { views = views };
        }

        public async Task AddViewForPhoto(ViewsPhoto views) =>
            await _viewsPhotoCollection.InsertOneAsync(views);

        public bool DoesViewFromUserExists(string userId, string photoId)
        {
            return _viewsPhotoCollection.AsQueryable().Any(x => x.userId == userId && x.photoId == photoId);
        }


        public List<(string, int, int)> GetPhotoViewStat(string photoId)
        {
            var dateGrouped = new List<(string, int, int)>();

            var months = Enumerable.Range(1, 12).Select(x => new
            {
                year = DateTime.Now.Year,
                month = x
            });

            var result = (from t in _viewsPhotoCollection.AsQueryable()
                          where t.photoId == photoId && t.created_at.Year == DateTime.Now.Year
                          group t by new
                          {
                              Year = t.created_at.Year,
                              Month = t.created_at.Month
                          } into g
                          select new
                          {
                              Month = g.Key.Month,
                              Year = g.Key.Year,
                              Total = g.Count()
                          }).ToList();


            foreach (var item in months)
            {
                dateGrouped.Add(((new DateTime(item.year, item.month, 1)).ToString("MMM"),
                    item.year,
                    result.Any(x => x.Month == item.month) ?
                    result.FirstOrDefault(x => x.Month == item.month).Total :
                    0));
            }

            return dateGrouped;
        }

        public List<(string, int, int)> GetPhotoLikeStat(string photoId)
        {
            var dateGrouped = new List<(string, int, int)>();

            var months = Enumerable.Range(1, 12).Select(x => new
            {
                year = DateTime.Now.Year,
                month = x
            });

            var result = (from t in _likesDisLikesPhotoCollection.AsQueryable()
                          where t.photo_id == photoId && t.isLikes && t.created_at.Year == DateTime.Now.Year
                          group t by new
                          {
                              Year = t.created_at.Year,
                              Month = t.created_at.Month
                          } into g
                          select new
                          {
                              Month = g.Key.Month,
                              Year = g.Key.Year,
                              Total = g.Count()
                          }).ToList();


            foreach (var item in months)
            {
                dateGrouped.Add(((new DateTime(item.year, item.month, 1)).ToString("MMM"),
                   item.year,
                   result.Any(x => x.Month == item.month) ?
                   result.FirstOrDefault(x => x.Month == item.month).Total :
                   0));
            }

            return dateGrouped;
        }

        public List<(string, int, int)> GetPhotoDisLikeStat(string photoId)
        {
            var dateGrouped = new List<(string, int, int)>();

            var months = Enumerable.Range(1, 12).Select(x => new
            {
                year = DateTime.Now.Year,
                month = x
            });

            var result = (from t in _likesDisLikesPhotoCollection.AsQueryable()
                          where t.photo_id == photoId && t.disLikes && t.created_at.Year == DateTime.Now.Year
                          group t by new
                          {
                              Year = t.created_at.Year,
                              Month = t.created_at.Month
                          } into g
                          select new
                          {
                              Month = g.Key.Month,
                              Year = g.Key.Year,
                              Total = g.Count()
                          }).ToList();


            foreach (var item in months)
            {
                dateGrouped.Add(((new DateTime(item.year, item.month, 1)).ToString("MMM"),
                  item.year,
                  result.Any(x => x.Month == item.month) ?
                  result.FirstOrDefault(x => x.Month == item.month).Total :
                  0));
            }

            return dateGrouped;
        }

        public List<(string, int, int, int, int)> GetPhotoUploadStats(string userId, int year)
        {
            var dateGrouped = new List<(string, int, int, int, int)>();

            var months = Enumerable.Range(1, 12).Select(x => new
            {
                year = year,
                month = x
            });

            var allCount = _photosCollection.AsQueryable().Where(x => x.userId == userId).Count();

            var countForUser = _photosCollection.AsQueryable().Where(x => x.userId == userId && x.created_at.Year == year).Count();

            var result = (from t in _photosCollection.AsQueryable()
                          where t.userId == userId && t.created_at.Year == year
                          group t by new
                          {
                              Year = t.created_at.Year,
                              Month = t.created_at.Month
                          } into g
                          select new
                          {
                              Month = g.Key.Month,
                              Year = g.Key.Year,
                              Total = g.Count()
                          }).ToList();


            foreach (var item in months)
            {
                dateGrouped.Add(((new DateTime(item.year, item.month, 1)).ToString("MMM"),
                  item.year,
                  result.Any(x => x.Month == item.month) ? result.FirstOrDefault(x => x.Month == item.month)!.Total : 0,
                  countForUser,
                  allCount
                  ));
            }

            return dateGrouped;
        }

        public async Task<long> GetPhotoUploadCount(string userId) => await _photosCollection.Find(x => x.userId == userId).CountDocumentsAsync();
    }
}
