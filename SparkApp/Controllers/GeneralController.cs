using Asp.Versioning;
using SparkApp.APIModel.General;
using SparkApp.APIModel.User;
using SparkApp.Extensions;
using SparkApp.Helper;
using SparkService.Models;
using SparkService.Services;
using MailKit.Net.Imap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using Org.BouncyCastle.Utilities;
using System;
using System.Drawing;
using System.Net.Http.Headers;
using System.Security.Claims;
using static System.Net.Mime.MediaTypeNames;

namespace SparkApp.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/general")]
    public class GeneralController : Controller
    {
        private readonly ILogger<GeneralController> _logger;
        private readonly FileService _fileService;
        private readonly UsersService _usersService;
        private readonly TraitsService _traitsService;
        private readonly InterestsService _interestsService;
        private readonly PhotosService _photosService;
        private readonly SubscriptionService _subscriptionService;

        public GeneralController(ILogger<GeneralController> logger, FileService fileService, UsersService usersService, TraitsService traitsService, InterestsService interestsService, PhotosService photosService, SubscriptionService subscriptionService) =>
            (_logger, _fileService, _usersService, _traitsService, _interestsService, _photosService, _subscriptionService) = (logger, fileService, usersService, traitsService, interestsService, photosService, subscriptionService);

        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("genders")]
        public ActionResult<ResponseModel<IEnumerable<dynamic>>> Genders()
        {
            ResponseModel<IEnumerable<dynamic>> responseModel = new ResponseModel<IEnumerable<dynamic>>();

            var genderTypes = Enum.GetNames(typeof(Gender)).Select(x => new { text = x, value = x });

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = genderTypes;
            return Ok(responseModel);
        }

        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("education-level")]
        public ActionResult<ResponseModel<IEnumerable<dynamic>>> EducationLevel()
        {
            ResponseModel<IEnumerable<dynamic>> responseModel = new ResponseModel<IEnumerable<dynamic>>();

            List<dynamic> educationalLevels = new List<dynamic>();

            foreach (object item in Enum.GetValues(typeof(EducationLevel)))
            {
                educationalLevels.Add(new { text = ((EducationLevel)item).GetDescription(), value = ((EducationLevel)item).GetDescription() });
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = educationalLevels;
            return Ok(responseModel);
        }

        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("relationship-goals")]
        public ActionResult<ResponseModel<IEnumerable<dynamic>>> RelationshipGoals()
        {
            ResponseModel<IEnumerable<dynamic>> responseModel = new ResponseModel<IEnumerable<dynamic>>();

            List<dynamic> relationshipGoals = new List<dynamic>();

            foreach (object item in Enum.GetValues(typeof(RelationshipGoals)))
            {
                relationshipGoals.Add(new { text = ((RelationshipGoals)item).GetDescription(), value = ((RelationshipGoals)item).GetDescription() });
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = relationshipGoals;
            return Ok(responseModel);
        }


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("iam")]
        public ActionResult<ResponseModel<IEnumerable<dynamic>>> Iam()
        {
            ResponseModel<IEnumerable<dynamic>> responseModel = new ResponseModel<IEnumerable<dynamic>>();

            List<dynamic> iamTypes = new List<dynamic>();

            foreach (object item in Enum.GetValues(typeof(Iam)))
            {
                iamTypes.Add(new { text = ((Iam)item).GetDescription(), value = ((Iam)item).GetDescription() });
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = iamTypes;
            return Ok(responseModel);
        }

        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("seeking")]
        public ActionResult<ResponseModel<IEnumerable<dynamic>>> Seeking()
        {
            ResponseModel<IEnumerable<dynamic>> responseModel = new ResponseModel<IEnumerable<dynamic>>();

            List<dynamic> SeekingItems = new List<dynamic>();

            foreach (object item in Enum.GetValues(typeof(Seeking)))
            {
                SeekingItems.Add(new { text = ((Seeking)item).GetDescription(), value = ((Seeking)item).GetDescription() });
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = SeekingItems;
            return Ok(responseModel);
        }


        [Authorize]
        [MapToApiVersion(1)]
        [HttpPost("upload")]
        public async Task<IActionResult> FileUpload(FileUploadCreateRequest model)
        {
            ResponseModel<FileResponseModel> responseModel = new ResponseModel<FileResponseModel>();

            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var username = claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value;
            var userId = claimsIdentity?.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

            string[] allowedMimeTypes = { "image/jpeg", "image/png", "image/gif" };
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

            var fileName = ContentDispositionHeaderValue.Parse(model.File.ContentDisposition).FileName.Trim('"');
            string ext = new DirectoryInfo(fileName).Extension;

            if (model.File == null)
            {
                _logger.LogError($"SparkApp.Controllers.GeneralController.FileUpload Error = No file avaliable.");
                throw new Exception("No file uploaded.");
            }

            if (!allowedMimeTypes.Contains(model.File.ContentType) && !allowedExtensions.Contains(ext))
            {
                _logger.LogError($"SparkApp.Controllers.GeneralController.FileUpload Error = Invalid file");
                throw new Exception("Invalid file, allowed file type are jpg, jpeg, png, gif");
            }

            FileResponseModel filesResult = new FileResponseModel();

            if (model.type == "image-profile")
            {

                Guid newFileName = Guid.NewGuid();
                var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), "FileStore", userId!, "profile");

                if (!Directory.Exists(pathToSave))
                {
                    Directory.CreateDirectory(pathToSave);
                }

                SparkService.Models.File uploadedFile = new SparkService.Models.File();

                uploadedFile.originalName = fileName;
                filesResult.name = fileName;

                uploadedFile.name = string.Format("{0}{1}", newFileName.ToString(), ext);

                using (var image = (Bitmap)System.Drawing.Image.FromStream(model.File.OpenReadStream()))
                {
                    if (!Directory.Exists(Path.Combine(pathToSave, "original")))
                    {
                        Directory.CreateDirectory(Path.Combine(pathToSave, "original"));
                    }
                    var fullPath = Path.Combine(pathToSave, "original", string.Format("{0}{1}", newFileName.ToString(), ext));
                    image.Save(fullPath);
                    uploadedFile.path_original = fullPath;
                    uploadedFile.query_original = string.Format("/{0}/{1}/{2}/{3}", userId!, "profile", "original", string.Format("{0}{1}", newFileName.ToString(), ext));
                    filesResult.original = string.Format("{0}://{1}/Store/{2}/{3}/{4}/{5}", Request.Scheme, Request.Host.Value.ToString(), userId!, "profile", "original", string.Format("{0}{1}", newFileName.ToString(), ext));
                }

                using (var image = (Bitmap)System.Drawing.Image.FromStream(model.File.OpenReadStream()))
                {
                    if (!Directory.Exists(Path.Combine(pathToSave, "400x320")))
                    {
                        Directory.CreateDirectory(Path.Combine(pathToSave, "400x320"));
                    }
                    var fullPath = Path.Combine(pathToSave, "400x320", string.Format("{0}_400x320{1}", newFileName.ToString(), ext));
                    ImageHandler.Save(image, 480, 320, 100L, fullPath);
                    uploadedFile.path_480x320 = fullPath;
                    uploadedFile.query_480x320 = string.Format("/{0}/{1}/{2}/{3}", userId!, "profile", "400x320", string.Format("{0}_400x320{1}", newFileName.ToString(), ext));
                    filesResult.d480x320 = string.Format("{0}://{1}/Store/{2}/{3}/{4}/{5}", Request.Scheme, Request.Host.Value.ToString(), userId!, "profile", "400x320", string.Format("{0}_400x320{1}", newFileName.ToString(), ext));
                }

                using (var image = (Bitmap)System.Drawing.Image.FromStream(model.File.OpenReadStream()))
                {
                    if (!Directory.Exists(Path.Combine(pathToSave, "300x300")))
                    {
                        Directory.CreateDirectory(Path.Combine(pathToSave, "300x300"));
                    }
                    var fullPath = Path.Combine(pathToSave, "300x300", string.Format("{0}_300x300{1}", newFileName.ToString(), ext));
                    ImageHandler.Save(image, 300, 300, 100L, fullPath);
                    uploadedFile.path_300x300 = fullPath;
                    uploadedFile.query_300x300 = string.Format("/{0}/{1}/{2}/{3}", userId!, "profile", "300x300", string.Format("{0}_300x300{1}", newFileName.ToString(), ext));
                    filesResult.d300x300 = string.Format("{0}://{1}/Store/{2}/{3}/{4}/{5}", Request.Scheme, Request.Host.Value.ToString(), userId!, "profile", "300x300", string.Format("{0}_300x300{1}", newFileName.ToString(), ext));
                }

                using (var image = (Bitmap)System.Drawing.Image.FromStream(model.File.OpenReadStream()))
                {
                    if (!Directory.Exists(Path.Combine(pathToSave, "100x100")))
                    {
                        Directory.CreateDirectory(Path.Combine(pathToSave, "100x100"));
                    }
                    var fullPath = Path.Combine(pathToSave, "100x100", string.Format("{0}_100x100{1}", newFileName.ToString(), ext));
                    ImageHandler.Save(image, 100, 100, 100L, fullPath);
                    uploadedFile.path_100x100 = fullPath;
                    uploadedFile.query_100x100 = string.Format("/{0}/{1}/{2}/{3}", userId!, "profile", "100x100", string.Format("{0}_100x100{1}", newFileName.ToString(), ext));
                    filesResult.d100x100 = string.Format("{0}://{1}/Store/{2}/{3}/{4}/{5}", Request.Scheme, Request.Host.Value.ToString(), userId!, "profile", "100x100", string.Format("{0}_100x100{1}", newFileName.ToString(), ext));
                }

                using (var image = (Bitmap)System.Drawing.Image.FromStream(model.File.OpenReadStream()))
                {
                    if (!Directory.Exists(Path.Combine(pathToSave, "32x32")))
                    {
                        Directory.CreateDirectory(Path.Combine(pathToSave, "32x32"));
                    }
                    var fullPath = Path.Combine(pathToSave, "32x32", string.Format("{0}_32x32{1}", newFileName.ToString(), ext));
                    ImageHandler.Save(image, 32, 32, 100L, fullPath);
                    uploadedFile.path_32x32 = fullPath;
                    uploadedFile.query_32x32 = string.Format("/{0}/{1}/{2}/{3}", userId!, "profile", "32x32", string.Format("{0}_32x32{1}", newFileName.ToString(), ext));
                    filesResult.d32x32 = string.Format("{0}://{1}/Store/{2}/{3}/{4}/{5}", Request.Scheme, Request.Host.Value.ToString(), userId!, "profile", "32x32", string.Format("{0}_32x32{1}", newFileName.ToString(), ext));
                }

                using (var image = (Bitmap)System.Drawing.Image.FromStream(model.File.OpenReadStream()))
                {
                    if (!Directory.Exists(Path.Combine(pathToSave, "16x16")))
                    {
                        Directory.CreateDirectory(Path.Combine(pathToSave, "16x16"));
                    }
                    var fullPath = Path.Combine(pathToSave, "16x16", string.Format("{0}_16x16{1}", newFileName.ToString(), ext));
                    ImageHandler.Save(image, 16, 16, 100L, fullPath);
                    uploadedFile.path_16x16 = fullPath;
                    uploadedFile.query_16x16 = string.Format("/{0}/{1}/{2}/{3}", userId!, "profile", "16x16", string.Format("{0}_16x16{1}", newFileName.ToString(), ext));
                    filesResult.d16x16 = string.Format("{0}://{1}/Store/{2}/{3}/{4}/{5}", Request.Scheme, Request.Host.Value.ToString(), userId!, "profile", "16x16", string.Format("{0}_16x16{1}", newFileName.ToString(), ext));
                }

                uploadedFile.type = model.File.ContentType;
                filesResult.type = model.File.ContentType;
                uploadedFile.size = ((model.File.Length / 1024f) / 1024f);
                uploadedFile.created_at = DateTime.Now.ToUniversalTime();

                await _fileService.CreateAsync(uploadedFile);

                filesResult.id = uploadedFile.Id!;
            }
            else if (model.type == "image-user-gallery")
            {

                var subscription = _subscriptionService.GetSubscription(userId!);
                if (subscription is null)
                {
                    _logger.LogError($"SparkApp.Controllers.UserController.FileUpload Error = no subscription not found");
                    throw new Exception($"no subscription not found");
                }

                var consumedGallerSizeByUser = _photosService.GetUserGallerySize(userId!);
                if (subscription.Plan.storage < consumedGallerSizeByUser)
                {
                    _logger.LogError($"SparkApp.Controllers.UserController.FileUpload Error = We cannot proceed with your file, as subscription plan storage limitation exceeded. ");
                    throw new Exception($"We cannot proceed with your file, as subscription plan storage limitation exceeded.");
                }


                Guid newFileName = Guid.NewGuid();

                var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), "FileStore", userId!, "gallery", newFileName.ToString());

                if (!Directory.Exists(pathToSave))
                {
                    Directory.CreateDirectory(pathToSave);
                }

                SparkService.Models.File uploadedFile = new SparkService.Models.File();

                uploadedFile.originalName = fileName;
                filesResult.name = fileName;

                uploadedFile.name = string.Format("{0}{1}", newFileName.ToString(), ext);

                using (var image = (Bitmap)System.Drawing.Image.FromStream(model.File.OpenReadStream()))
                {
                    if (!Directory.Exists(Path.Combine(pathToSave, "original")))
                    {
                        Directory.CreateDirectory(Path.Combine(pathToSave, "original"));
                    }
                    var fullPath = Path.Combine(pathToSave, "original", string.Format("{0}{1}", newFileName.ToString(), ext));
                    image.Save(fullPath);
                    uploadedFile.path_original = fullPath;
                    uploadedFile.query_original = string.Format("/{0}/{1}/{2}/{3}/{4}", userId!, "gallery", newFileName.ToString(), "original", string.Format("{0}{1}", newFileName.ToString(), ext));
                    filesResult.original = string.Format("{0}://{1}/Store/{2}/{3}/{4}/{5}/{6}", Request.Scheme, Request.Host.Value.ToString(), userId!, "gallery", newFileName.ToString(), "original", string.Format("{0}{1}", newFileName.ToString(), ext));
                }

                using (var image = (Bitmap)System.Drawing.Image.FromStream(model.File.OpenReadStream()))
                {
                    if (!Directory.Exists(Path.Combine(pathToSave, "400x320")))
                    {
                        Directory.CreateDirectory(Path.Combine(pathToSave, "400x320"));
                    }
                    var fullPath = Path.Combine(pathToSave, "400x320", string.Format("{0}_400x320{1}", newFileName.ToString(), ext));
                    ImageHandler.Save(image, 480, 320, 100L, fullPath);
                    uploadedFile.path_480x320 = fullPath;
                    uploadedFile.query_480x320 = string.Format("/{0}/{1}/{2}/{3}/{4}", userId!, "gallery", newFileName.ToString(), "400x320", string.Format("{0}_400x320{1}", newFileName.ToString(), ext));
                    filesResult.d480x320 = string.Format("{0}://{1}/Store/{2}/{3}/{4}/{5}/{6}", Request.Scheme, Request.Host.Value.ToString(), userId!, "gallery", newFileName.ToString(), "400x320", string.Format("{0}_400x320{1}", newFileName.ToString(), ext));
                }

                using (var image = (Bitmap)System.Drawing.Image.FromStream(model.File.OpenReadStream()))
                {
                    if (!Directory.Exists(Path.Combine(pathToSave, "300x300")))
                    {
                        Directory.CreateDirectory(Path.Combine(pathToSave, "300x300"));
                    }
                    var fullPath = Path.Combine(pathToSave, "300x300", string.Format("{0}_300x300{1}", newFileName.ToString(), ext));
                    ImageHandler.Save(image, 300, 300, 100L, fullPath);
                    uploadedFile.path_300x300 = fullPath;
                    uploadedFile.query_300x300 = string.Format("/{0}/{1}/{2}/{3}/{4}", userId!, "gallery", newFileName.ToString(), "300x300", string.Format("{0}_300x300{1}", newFileName.ToString(), ext));
                    filesResult.d300x300 = string.Format("{0}://{1}/Store/{2}/{3}/{4}/{5}/{6}", Request.Scheme, Request.Host.Value.ToString(), userId!, "gallery", newFileName.ToString(), "300x300", string.Format("{0}_300x300{1}", newFileName.ToString(), ext));
                }

                using (var image = (Bitmap)System.Drawing.Image.FromStream(model.File.OpenReadStream()))
                {
                    if (!Directory.Exists(Path.Combine(pathToSave, "100x100")))
                    {
                        Directory.CreateDirectory(Path.Combine(pathToSave, "100x100"));
                    }
                    var fullPath = Path.Combine(pathToSave, "100x100", string.Format("{0}_100x100{1}", newFileName.ToString(), ext));
                    ImageHandler.Save(image, 100, 100, 100L, fullPath);
                    uploadedFile.path_100x100 = fullPath;
                    uploadedFile.query_100x100 = string.Format("/{0}/{1}/{2}/{3}/{4}", userId!, "gallery", newFileName.ToString(), "100x100", string.Format("{0}_100x100{1}", newFileName.ToString(), ext));
                    filesResult.d100x100 = string.Format("{0}://{1}/Store/{2}/{3}/{4}/{5}/{6}", Request.Scheme, Request.Host.Value.ToString(), userId!, "gallery", newFileName.ToString(), "100x100", string.Format("{0}_100x100{1}", newFileName.ToString(), ext));
                }

                using (var image = (Bitmap)System.Drawing.Image.FromStream(model.File.OpenReadStream()))
                {
                    if (!Directory.Exists(Path.Combine(pathToSave, "32x32")))
                    {
                        Directory.CreateDirectory(Path.Combine(pathToSave, "32x32"));
                    }
                    var fullPath = Path.Combine(pathToSave, "32x32", string.Format("{0}_32x32{1}", newFileName.ToString(), ext));
                    ImageHandler.Save(image, 32, 32, 100L, fullPath);
                    uploadedFile.path_32x32 = fullPath;
                    uploadedFile.query_32x32 = string.Format("/{0}/{1}/{2}/{3}/{4}", userId!, "gallery", newFileName.ToString(), "32x32", string.Format("{0}_32x32{1}", newFileName.ToString(), ext));
                    filesResult.d32x32 = string.Format("{0}://{1}/Store/{2}/{3}/{4}/{5}/{6}", Request.Scheme, Request.Host.Value.ToString(), userId!, "gallery", newFileName.ToString(), "32x32", string.Format("{0}_32x32{1}", newFileName.ToString(), ext));
                }

                using (var image = (Bitmap)System.Drawing.Image.FromStream(model.File.OpenReadStream()))
                {
                    if (!Directory.Exists(Path.Combine(pathToSave, "16x16")))
                    {
                        Directory.CreateDirectory(Path.Combine(pathToSave, "16x16"));
                    }
                    var fullPath = Path.Combine(pathToSave, "16x16", string.Format("{0}_16x16{1}", newFileName.ToString(), ext));
                    ImageHandler.Save(image, 16, 16, 100L, fullPath);
                    uploadedFile.path_16x16 = fullPath;

                    uploadedFile.query_16x16 = string.Format("/{0}/{1}/{2}/{3}/{4}", userId!, "gallery", newFileName.ToString(), "16x16", string.Format("{0}_16x16{1}", newFileName.ToString(), ext));
                    filesResult.d16x16 = string.Format("{0}://{1}/Store/{2}/{3}/{4}/{5}/{6}", Request.Scheme, Request.Host.Value.ToString(), userId!, "gallery", newFileName.ToString(), "16x16", string.Format("{0}_16x16{1}", newFileName.ToString(), ext));
                }

                uploadedFile.type = model.File.ContentType;
                filesResult.type = model.File.ContentType;
                uploadedFile.size = ((model.File.Length / 1024f) / 1024f);
                uploadedFile.created_at = DateTime.Now.ToUniversalTime();

                await _fileService.CreateAsync(uploadedFile);

                filesResult.id = uploadedFile.Id!;
            }

            responseModel.Data = filesResult;
            responseModel.Message = "File uploaded";
            responseModel.Success = true;

            return Ok(responseModel);
        }

        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("traits")]
        public ActionResult<ResponseModel<object>> Traits()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var traits = _traitsService.GetAllTraits();

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = traits;
            return Ok(responseModel);
        }


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("interest-categories")]
        public ActionResult<ResponseModel<object>> InterestCategories()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var interests = _interestsService.GetInterestCategories();

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = interests;
            return Ok(responseModel);
        }

        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("body-type")]
        public ActionResult<ResponseModel<object>> BodyType()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var bodyType = new List<object>();

            bodyType.Add(new { text = "Slender", value = "Slender" });
            bodyType.Add(new { text = "Big and beautiful", value = "Big and beautiful" });
            bodyType.Add(new { text = "Curvy", value = "Curvy" });
            bodyType.Add(new { text = "About average", value = "About average" });
            bodyType.Add(new { text = "Athletic and toned", value = "Athletic and toned" });
            bodyType.Add(new { text = "Full-figured", value = "Full-figured" });
            bodyType.Add(new { text = "Heavyset", value = "Heavyset" });
            bodyType.Add(new { text = "A few extra pounds", value = "A few extra pounds" });
            bodyType.Add(new { text = "Stocky", value = "Stocky" });

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = bodyType;
            return Ok(responseModel);
        }

        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("timezones")]
        public ActionResult<ResponseModel<object>> Timezones()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var timezones = new List<object>();

            timezones.Add(new { text = "GMT-12:00", value = "Etc/GMT+12" });
            timezones.Add(new { text = "GMT-11:00", value = "Etc/GMT+11" });
            timezones.Add(new { text = "West Samoa Time", value = "MIT" });
            timezones.Add(new { text = "Eastern Standard Time", value = "America/New_York" });
            timezones.Add(new { text = "Asia/Calcutta", value = "Asia/Calcutta" });

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = timezones;
            return Ok(responseModel);
        }


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("languages")]
        public ActionResult<ResponseModel<object>> Languages()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var languages = new List<object>();

            languages.Add(new { text = "English", value = "English" });
            languages.Add(new { text = "German", value = "German" });
            languages.Add(new { text = "French", value = "French" });

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = languages;
            return Ok(responseModel);
        }


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("countries")]
        public ActionResult<ResponseModel<object>> Countries()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var countries = new List<object>();

            countries.Add(new { text = "Afghanistan", value = "Afghanistan" });
            countries.Add(new { text = "Åland Islands", value = "Åland Islands" });
            countries.Add(new { text = "Albania", value = "Albania" });
            countries.Add(new { text = "Algeria", value = "Algeria" });
            countries.Add(new { text = "American Samoa", value = "American Samoa" });
            countries.Add(new { text = "Andorra", value = "Andorra" });
            countries.Add(new { text = "Angola", value = "Angola" });
            countries.Add(new { text = "Anguilla", value = "Anguilla" });
            countries.Add(new { text = "Antarctica", value = "Antarctica" });
            countries.Add(new { text = "Antigua and Barbuda", value = "Antigua and Barbuda" });
            countries.Add(new { text = "Argentina", value = "Argentina" });
            countries.Add(new { text = "Armenia", value = "Armenia" });
            countries.Add(new { text = "Aruba", value = "Aruba" });
            countries.Add(new { text = "Australia", value = "Australia" });
            countries.Add(new { text = "Austria", value = "Austria" });
            countries.Add(new { text = "Azerbaijan", value = "Azerbaijan" });
            countries.Add(new { text = "Bahamas", value = "Bahamas" });
            countries.Add(new { text = "Bahrain", value = "Bahrain" });
            countries.Add(new { text = "Bangladesh", value = "Bangladesh" });
            countries.Add(new { text = "Barbados", value = "Barbados" });
            countries.Add(new { text = "Belarus", value = "Belarus" });
            countries.Add(new { text = "Belgium", value = "Belgium" });
            countries.Add(new { text = "Belize", value = "Belize" });
            countries.Add(new { text = "Benin", value = "Benin" });
            countries.Add(new { text = "Bermuda", value = "Bermuda" });
            countries.Add(new { text = "Bhutan", value = "Bhutan" });
            countries.Add(new { text = "Bolivia", value = "Bolivia" });
            countries.Add(new { text = "Bosnia and Herzegovina", value = "Bosnia and Herzegovina" });
            countries.Add(new { text = "Botswana", value = "Botswana" });
            countries.Add(new { text = "Bouvet Island", value = "Bouvet Island" });
            countries.Add(new { text = "Brazil", value = "Brazil" });
            countries.Add(new { text = "British Indian Ocean Territory", value = "British Indian Ocean Territory" });
            countries.Add(new { text = "Brunei Darussalam", value = "Brunei Darussalam" });
            countries.Add(new { text = "Bulgaria", value = "Bulgaria" });
            countries.Add(new { text = "Burkina Faso", value = "Burkina Faso" });
            countries.Add(new { text = "Burundi", value = "Burundi" });
            countries.Add(new { text = "Cambodia", value = "Cambodia" });
            countries.Add(new { text = "Cameroon", value = "Cameroon" });
            countries.Add(new { text = "Canada", value = "Canada" });
            countries.Add(new { text = "Cape Verde", value = "Cape Verde" });
            countries.Add(new { text = "Cayman Islands", value = "Cayman Islands" });
            countries.Add(new { text = "Central African Republic", value = "Central African Republic" });
            countries.Add(new { text = "Chad", value = "Chad" });
            countries.Add(new { text = "Chile", value = "Chile" });
            countries.Add(new { text = "China", value = "China" });
            countries.Add(new { text = "Christmas Island", value = "Christmas Island" });
            countries.Add(new { text = "Cocos (Keeling) Islands", value = "Cocos (Keeling) Islands" });
            countries.Add(new { text = "Colombia", value = "Colombia" });
            countries.Add(new { text = "Comoros", value = "Comoros" });
            countries.Add(new { text = "Congo", value = "Congo" });
            countries.Add(new { text = "Congo, The Democratic Republic of The", value = "Congo, The Democratic Republic of The" });
            countries.Add(new { text = "Cook Islands", value = "Cook Islands" });
            countries.Add(new { text = "Costa Rica", value = "Costa Rica" });
            countries.Add(new { text = "Cote D'ivoire", value = "Cote D'ivoire" });
            countries.Add(new { text = "Croatia", value = "Croatia" });
            countries.Add(new { text = "Cuba", value = "Cuba" });
            countries.Add(new { text = "Cyprus", value = "Cyprus" });
            countries.Add(new { text = "Czech Republic", value = "Czech Republic" });
            countries.Add(new { text = "Denmark", value = "Denmark" });
            countries.Add(new { text = "Djibouti", value = "Djibouti" });
            countries.Add(new { text = "Dominica", value = "Dominica" });
            countries.Add(new { text = "Dominican Republic", value = "Dominican Republic" });
            countries.Add(new { text = "Ecuador", value = "Ecuador" });
            countries.Add(new { text = "Egypt", value = "Egypt" });
            countries.Add(new { text = "El Salvador", value = "El Salvador" });
            countries.Add(new { text = "Equatorial Guinea", value = "Equatorial Guinea" });
            countries.Add(new { text = "Eritrea", value = "Eritrea" });
            countries.Add(new { text = "Estonia", value = "Estonia" });
            countries.Add(new { text = "Ethiopia", value = "Ethiopia" });
            countries.Add(new { text = "Falkland Islands (Malvinas)", value = "Falkland Islands (Malvinas)" });
            countries.Add(new { text = "Faroe Islands", value = "Faroe Islands" });
            countries.Add(new { text = "Fiji", value = "Fiji" });
            countries.Add(new { text = "Finland", value = "Finland" });
            countries.Add(new { text = "France", value = "France" });
            countries.Add(new { text = "French Guiana", value = "French Guiana" });
            countries.Add(new { text = "French Polynesia", value = "French Polynesia" });
            countries.Add(new { text = "French Southern Territories", value = "French Southern Territories" });
            countries.Add(new { text = "Gabon", value = "Gabon" });
            countries.Add(new { text = "Gambia", value = "Gambia" });
            countries.Add(new { text = "Georgia", value = "Georgia" });
            countries.Add(new { text = "Germany", value = "Germany" });
            countries.Add(new { text = "Ghana", value = "Ghana" });
            countries.Add(new { text = "Gibraltar", value = "Gibraltar" });
            countries.Add(new { text = "Greece", value = "Greece" });
            countries.Add(new { text = "Greenland", value = "Greenland" });
            countries.Add(new { text = "Grenada", value = "Grenada" });
            countries.Add(new { text = "Guadeloupe", value = "Guadeloupe" });
            countries.Add(new { text = "Guam", value = "Guam" });
            countries.Add(new { text = "Guatemala", value = "Guatemala" });
            countries.Add(new { text = "Guernsey", value = "Guernsey" });
            countries.Add(new { text = "Guinea", value = "Guinea" });
            countries.Add(new { text = "Guinea-bissau", value = "Guinea-bissau" });
            countries.Add(new { text = "Guyana", value = "Guyana" });
            countries.Add(new { text = "Haiti", value = "Haiti" });
            countries.Add(new { text = "Heard Island and Mcdonald Islands", value = "Heard Island and Mcdonald Islands" });
            countries.Add(new { text = "Holy See (Vatican City State)", value = "Holy See (Vatican City State)" });
            countries.Add(new { text = "Honduras", value = "Honduras" });
            countries.Add(new { text = "Hong Kong", value = "Hong Kong" });
            countries.Add(new { text = "Hungary", value = "Hungary" });
            countries.Add(new { text = "Iceland", value = "Iceland" });
            countries.Add(new { text = "India", value = "India" });
            countries.Add(new { text = "Indonesia", value = "Indonesia" });
            countries.Add(new { text = "Iran, Islamic Republic of", value = "Iran, Islamic Republic of" });
            countries.Add(new { text = "Iraq", value = "Iraq" });
            countries.Add(new { text = "Ireland", value = "Ireland" });
            countries.Add(new { text = "Isle of Man", value = "Isle of Man" });
            countries.Add(new { text = "Israel", value = "Israel" });
            countries.Add(new { text = "Italy", value = "Italy" });
            countries.Add(new { text = "Jamaica", value = "Jamaica" });
            countries.Add(new { text = "Japan", value = "Japan" });
            countries.Add(new { text = "Jersey", value = "Jersey" });
            countries.Add(new { text = "Jordan", value = "Jordan" });
            countries.Add(new { text = "Kazakhstan", value = "Kazakhstan" });
            countries.Add(new { text = "Kenya", value = "Kenya" });
            countries.Add(new { text = "Kiribati", value = "Kiribati" });
            countries.Add(new { text = "Korea, Democratic People's Republic of", value = "Korea, Democratic People's Republic of" });
            countries.Add(new { text = "Korea, Republic of", value = "Korea, Republic of" });
            countries.Add(new { text = "Kuwait", value = "Kuwait" });
            countries.Add(new { text = "Kyrgyzstan", value = "Kyrgyzstan" });
            countries.Add(new { text = "Lao People's Democratic Republic", value = "Lao People's Democratic Republic" });
            countries.Add(new { text = "Latvia", value = "Latvia" });
            countries.Add(new { text = "Lebanon", value = "Lebanon" });
            countries.Add(new { text = "Lesotho", value = "Lesotho" });
            countries.Add(new { text = "Liberia", value = "Liberia" });
            countries.Add(new { text = "Libyan Arab Jamahiriya", value = "Libyan Arab Jamahiriya" });
            countries.Add(new { text = "Liechtenstein", value = "Liechtenstein" });
            countries.Add(new { text = "Lithuania", value = "Lithuania" });
            countries.Add(new { text = "Luxembourg", value = "Luxembourg" });
            countries.Add(new { text = "Macao", value = "Macao" });
            countries.Add(new { text = "Macedonia, The Former Yugoslav Republic of", value = "Macedonia, The Former Yugoslav Republic of" });
            countries.Add(new { text = "Madagascar", value = "Madagascar" });
            countries.Add(new { text = "Malawi", value = "Malawi" });
            countries.Add(new { text = "Malaysia", value = "Malaysia" });
            countries.Add(new { text = "Maldives", value = "Maldives" });
            countries.Add(new { text = "Mali", value = "Mali" });
            countries.Add(new { text = "Malta", value = "Malta" });
            countries.Add(new { text = "Marshall Islands", value = "Marshall Islands" });
            countries.Add(new { text = "Martinique", value = "Martinique" });
            countries.Add(new { text = "Mauritania", value = "Mauritania" });
            countries.Add(new { text = "Mauritius", value = "Mauritius" });
            countries.Add(new { text = "Mayotte", value = "Mayotte" });
            countries.Add(new { text = "Mexico", value = "Mexico" });
            countries.Add(new { text = "Micronesia, Federated States of", value = "Micronesia, Federated States of" });
            countries.Add(new { text = "Moldova, Republic of", value = "Moldova, Republic of" });
            countries.Add(new { text = "Monaco", value = "Monaco" });
            countries.Add(new { text = "Mongolia", value = "Mongolia" });
            countries.Add(new { text = "Montenegro", value = "Montenegro" });
            countries.Add(new { text = "Montserrat", value = "Montserrat" });
            countries.Add(new { text = "Morocco", value = "Morocco" });
            countries.Add(new { text = "Mozambique", value = "Mozambique" });
            countries.Add(new { text = "Myanmar", value = "Myanmar" });
            countries.Add(new { text = "Namibia", value = "Namibia" });
            countries.Add(new { text = "Nauru", value = "Nauru" });
            countries.Add(new { text = "Nepal", value = "Nepal" });
            countries.Add(new { text = "Netherlands", value = "Netherlands" });
            countries.Add(new { text = "Netherlands Antilles", value = "Netherlands Antilles" });
            countries.Add(new { text = "New Caledonia", value = "New Caledonia" });
            countries.Add(new { text = "New Zealand", value = "New Zealand" });
            countries.Add(new { text = "Nicaragua", value = "Nicaragua" });
            countries.Add(new { text = "Niger", value = "Niger" });
            countries.Add(new { text = "Nigeria", value = "Nigeria" });
            countries.Add(new { text = "Niue", value = "Niue" });
            countries.Add(new { text = "Norfolk Island", value = "Norfolk Island" });
            countries.Add(new { text = "Northern Mariana Islands", value = "Northern Mariana Islands" });
            countries.Add(new { text = "Norway", value = "Norway" });
            countries.Add(new { text = "Oman", value = "Oman" });
            countries.Add(new { text = "Pakistan", value = "Pakistan" });
            countries.Add(new { text = "Palau", value = "Palau" });
            countries.Add(new { text = "Palestinian Territory, Occupied", value = "Palestinian Territory, Occupied" });
            countries.Add(new { text = "Panama", value = "Panama" });
            countries.Add(new { text = "Papua New Guinea", value = "Papua New Guinea" });
            countries.Add(new { text = "Paraguay", value = "Paraguay" });
            countries.Add(new { text = "Peru", value = "Peru" });
            countries.Add(new { text = "Philippines", value = "Philippines" });
            countries.Add(new { text = "Pitcairn", value = "Pitcairn" });
            countries.Add(new { text = "Poland", value = "Poland" }); ;
            countries.Add(new { text = "Portugal", value = "Portugal" });
            countries.Add(new { text = "Puerto Rico", value = "Puerto Rico" });
            countries.Add(new { text = "Qatar", value = "Qatar" });
            countries.Add(new { text = "Reunion", value = "Reunion" });
            countries.Add(new { text = "Romania", value = "Romania" });
            countries.Add(new { text = "Russian Federation", value = "Russian Federation" });
            countries.Add(new { text = "Rwanda", value = "Rwanda" });
            countries.Add(new { text = "Saint Helena", value = "Saint Helena" });
            countries.Add(new { text = "Saint Kitts and Nevis", value = "Saint Kitts and Nevis" });
            countries.Add(new { text = "Saint Lucia", value = "Saint Lucia" });
            countries.Add(new { text = "Saint Pierre and Miquelon", value = "Saint Pierre and Miquelon" });
            countries.Add(new { text = "Saint Vincent and The Grenadines", value = "Saint Vincent and The Grenadines" });
            countries.Add(new { text = "Samoa", value = "Samoa" });
            countries.Add(new { text = "San Marino", value = "San Marino" });
            countries.Add(new { text = "Sao Tome and Principe", value = "Sao Tome and Principe" });
            countries.Add(new { text = "Saudi Arabia", value = "Saudi Arabia" });
            countries.Add(new { text = "Senegal", value = "Senegal" });
            countries.Add(new { text = "Serbia", value = "Serbia" });
            countries.Add(new { text = "Seychelles", value = "Seychelles" });
            countries.Add(new { text = "Sierra Leone", value = "Sierra Leone" });
            countries.Add(new { text = "Singapore", value = "Singapore" });
            countries.Add(new { text = "Slovakia", value = "Slovakia" });
            countries.Add(new { text = "Slovenia", value = "Slovenia" });
            countries.Add(new { text = "Solomon Islands", value = "Solomon Islands" });
            countries.Add(new { text = "Somalia", value = "Somalia" });
            countries.Add(new { text = "South Africa", value = "South Africa" });
            countries.Add(new { text = "South Georgia and The South Sandwich Islands", value = "South Georgia and The South Sandwich Islands" });
            countries.Add(new { text = "Spain", value = "Spain" });
            countries.Add(new { text = "Sri Lanka", value = "Sri Lanka" });
            countries.Add(new { text = "Sudan", value = "Sudan" });
            countries.Add(new { text = "Suriname", value = "Suriname" });
            countries.Add(new { text = "Svalbard and Jan Mayen", value = "Svalbard and Jan Mayen" });
            countries.Add(new { text = "Swaziland", value = "Swaziland" });
            countries.Add(new { text = "Sweden", value = "Sweden" });
            countries.Add(new { text = "Switzerland", value = "Switzerland" });
            countries.Add(new { text = "Syrian Arab Republic", value = "Syrian Arab Republic" });
            countries.Add(new { text = "Taiwan", value = "Taiwan" });
            countries.Add(new { text = "Tajikistan", value = "Tajikistan" });
            countries.Add(new { text = "Tanzania, United Republic of", value = "Tanzania, United Republic of" });
            countries.Add(new { text = "Thailand", value = "Thailand" });
            countries.Add(new { text = "Timor-leste", value = "Timor-leste" });
            countries.Add(new { text = "Togo", value = "Togo" });
            countries.Add(new { text = "Tokelau", value = "Tokelau" });
            countries.Add(new { text = "Tonga", value = "Tonga" });
            countries.Add(new { text = "Trinidad and Tobago", value = "Trinidad and Tobago" });
            countries.Add(new { text = "Tunisia", value = "Tunisia" });
            countries.Add(new { text = "Turkey", value = "Turkey" });
            countries.Add(new { text = "Turkmenistan", value = "Turkmenistan" });
            countries.Add(new { text = "Turks and Caicos Islands", value = "Turks and Caicos Islands" });
            countries.Add(new { text = "Tuvalu", value = "Tuvalu" });
            countries.Add(new { text = "Uganda", value = "Uganda" });
            countries.Add(new { text = "Ukraine", value = "Ukraine" });
            countries.Add(new { text = "United Arab Emirates", value = "United Arab Emirates" });
            countries.Add(new { text = "United Kingdom", value = "United Kingdom" });
            countries.Add(new { text = "United States", value = "United States" });
            countries.Add(new { text = "United States Minor Outlying Islands", value = "United States Minor Outlying Islands" });
            countries.Add(new { text = "Uruguay", value = "Uruguay" });
            countries.Add(new { text = "Uzbekistan", value = "Uzbekistan" });
            countries.Add(new { text = "Vanuatu", value = "Vanuatu" });
            countries.Add(new { text = "Venezuela", value = "Venezuela" });
            countries.Add(new { text = "Viet Nam", value = "Viet Nam" });
            countries.Add(new { text = "Virgin Islands, British", value = "Virgin Islands, British" });
            countries.Add(new { text = "Virgin Islands, U.S.", value = "Virgin Islands, U.S." });
            countries.Add(new { text = "Wallis and Futuna", value = "Wallis and Futuna" });
            countries.Add(new { text = "Western Sahara", value = "Western Sahara" });
            countries.Add(new { text = "Yemen", value = "Yemen" });
            countries.Add(new { text = "Zambia", value = "Zambia" });
            countries.Add(new { text = "Zimbabwe", value = "Zimbabwe" });

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = countries;
            return Ok(responseModel);
        }


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("height")]
        public ActionResult<ResponseModel<object>> Height()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var height = new List<object>();

            height.Add(new { text = "4'0", value = "4'0" });
            height.Add(new { text = "4'1", value = "4'1" });
            height.Add(new { text = "4'2", value = "4'2" });
            height.Add(new { text = "4'3", value = "4'3" });
            height.Add(new { text = "4'4", value = "4'4" });
            height.Add(new { text = "4'5", value = "4'5" });
            height.Add(new { text = "4'6", value = "4'6" });
            height.Add(new { text = "4'7", value = "4'7" });
            height.Add(new { text = "4'8", value = "4'8" });
            height.Add(new { text = "4'9", value = "4'9" });
            height.Add(new { text = "4'10", value = "4'10" });
            height.Add(new { text = "4'11", value = "4'11" });
            height.Add(new { text = "5'0", value = "5'0" });
            height.Add(new { text = "5'1", value = "5'1" });
            height.Add(new { text = "5'2", value = "5'2" });
            height.Add(new { text = "5'3", value = "5'3" });
            height.Add(new { text = "5'4", value = "5'4" });
            height.Add(new { text = "5'5", value = "5'5" });
            height.Add(new { text = "5'6", value = "5'6" });
            height.Add(new { text = "5'7", value = "5'7" });
            height.Add(new { text = "5'8", value = "5'8" });
            height.Add(new { text = "5'9", value = "5'9" });
            height.Add(new { text = "5'10", value = "5'10" });
            height.Add(new { text = "5'11", value = "5'11" });
            height.Add(new { text = "6'0", value = "6'0" });
            height.Add(new { text = "6'1", value = "6'1" });
            height.Add(new { text = "6'2", value = "6'2" });
            height.Add(new { text = "6'3", value = "6'3" });
            height.Add(new { text = "6'4", value = "6'4" });
            height.Add(new { text = "6'5", value = "6'5" });
            height.Add(new { text = "6'6", value = "6'6" });
            height.Add(new { text = "6'7", value = "6'7" });
            height.Add(new { text = "6'8", value = "6'8" });
            height.Add(new { text = "6'9", value = "6'9" });
            height.Add(new { text = "6'10", value = "6'10" });
            height.Add(new { text = "6'11", value = "6'11" });
            height.Add(new { text = "7'0", value = "7'0" });

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = height;
            return Ok(responseModel);
        }


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("income")]
        public ActionResult<ResponseModel<object>> Income()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var income = new List<object>();

            income.Add(new { text = "Under 50,000", value = "Under 50,000" });
            income.Add(new { text = "50,000-100,000", value = "50,000-100,000" });
            income.Add(new { text = "100,000-200,000", value = "100,000-200,000" });
            income.Add(new { text = "Over 200,000", value = "Over 200,000" });

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = income;
            return Ok(responseModel);
        }


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("martial-status")]
        public ActionResult<ResponseModel<object>> MartialStatus()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var martialStatus = new List<object>();

            martialStatus.Add(new { text = "Married", value = "Married" });
            martialStatus.Add(new { text = "Un-Married", value = "Un-Married" });

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = martialStatus;
            return Ok(responseModel);
        }


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("race")]
        public ActionResult<ResponseModel<object>> Race()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();

            var races = new List<object>();

            races.Add(new { text = "English", value = "White English" });
            races.Add(new { text = "Welsh", value = "White Welsh" });
            races.Add(new { text = "Scottish", value = "White Scottish" });
            races.Add(new { text = "Northern Irish", value = "White Northern Irish" });
            races.Add(new { text = "Irish", value = "White Irish" });
            races.Add(new { text = "Gypsy or Irish Traveller", value = "White Gypsy or Irish Traveller" });
            races.Add(new { text = "Any other White background", value = "White Other" });
            races.Add(new { text = "White and Black Caribbean", value = "Mixed White and Black Caribbean" });
            races.Add(new { text = "White and Black African", value = "Mixed White and Black African" });
            races.Add(new { text = "Any other Mixed or Multiple background", value = "Mixed White Other" });
            races.Add(new { text = "Indian", value = "Asian Indian" });
            races.Add(new { text = "Pakistani", value = "Asian Pakistani" });
            races.Add(new { text = "Bangladeshi", value = "Asian Bangladeshi" });
            races.Add(new { text = "Chinese", value = "Asian Chinese" });
            races.Add(new { text = "Any other Asian background", value = "Asian Other" });
            races.Add(new { text = "African", value = "Black African" });
            races.Add(new { text = "African American", value = "Black African American" });
            races.Add(new { text = "Caribbean", value = "Black Caribbean" });
            races.Add(new { text = "Any other Black background", value = "Black Other" });
            races.Add(new { text = "Arab", value = "Arab" });
            races.Add(new { text = "Hispanic", value = "Hispanic" });
            races.Add(new { text = "Latino", value = "Latino" });
            races.Add(new { text = "Native American", value = "Native American" });
            races.Add(new { text = "Pacific Islander", value = "Pacific Islander" });
            races.Add(new { text = "Any other ethnic group", value = "Other" });

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = races;
            return Ok(responseModel);
        }



        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("age")]
        public ActionResult<ResponseModel<object>> Age()
        {
            ResponseModel<object> responseModel = new ResponseModel<object>();
            var age = new List<object>();

            age.Add(new { text = "21", value = "21" });
            age.Add(new { text = "22", value = "22" });
            age.Add(new { text = "23", value = "23" });
            age.Add(new { text = "24", value = "24" });
            age.Add(new { text = "25", value = "25" });
            age.Add(new { text = "26", value = "26" });
            age.Add(new { text = "27", value = "27" });
            age.Add(new { text = "28", value = "28" });
            age.Add(new { text = "29", value = "29" });
            age.Add(new { text = "30", value = "30" });
            age.Add(new { text = "31", value = "31" });
            age.Add(new { text = "32", value = "32" });
            age.Add(new { text = "33", value = "33" });
            age.Add(new { text = "34", value = "34" });
            age.Add(new { text = "35", value = "35" });
            age.Add(new { text = "36", value = "36" });
            age.Add(new { text = "37", value = "37" });
            age.Add(new { text = "38", value = "38" });
            age.Add(new { text = "39", value = "39" });
            age.Add(new { text = "40", value = "40" });
            age.Add(new { text = "41", value = "41" });
            age.Add(new { text = "42", value = "42" });
            age.Add(new { text = "43", value = "43" });
            age.Add(new { text = "44", value = "44" });
            age.Add(new { text = "45", value = "45" });
            age.Add(new { text = "46", value = "46" });
            age.Add(new { text = "47", value = "47" });
            age.Add(new { text = "48", value = "48" });
            age.Add(new { text = "49", value = "49" });
            age.Add(new { text = "50", value = "50" });
            age.Add(new { text = "51", value = "51" });
            age.Add(new { text = "52", value = "52" });
            age.Add(new { text = "53", value = "53" });
            age.Add(new { text = "54", value = "54" });
            age.Add(new { text = "55", value = "55" });
            age.Add(new { text = "56", value = "56" });
            age.Add(new { text = "57", value = "57" });
            age.Add(new { text = "58", value = "58" });
            age.Add(new { text = "59", value = "59" });
            age.Add(new { text = "60", value = "60" });
            age.Add(new { text = "61", value = "61" });
            age.Add(new { text = "62", value = "62" });
            age.Add(new { text = "63", value = "63" });
            age.Add(new { text = "64", value = "64" });
            age.Add(new { text = "65", value = "65" });
            age.Add(new { text = "66", value = "66" });
            age.Add(new { text = "67", value = "67" });
            age.Add(new { text = "68", value = "68" });
            age.Add(new { text = "69", value = "69" });
            age.Add(new { text = "70", value = "70" });
            age.Add(new { text = "71", value = "71" });
            age.Add(new { text = "72", value = "72" });
            age.Add(new { text = "73", value = "73" });
            age.Add(new { text = "74", value = "74" });
            age.Add(new { text = "75", value = "75" });
            age.Add(new { text = "76", value = "76" });
            age.Add(new { text = "77", value = "77" });
            age.Add(new { text = "78", value = "78" });
            age.Add(new { text = "79", value = "79" });
            age.Add(new { text = "80", value = "80" }); ;
            age.Add(new { text = "81", value = "81" });
            age.Add(new { text = "82", value = "82" });
            age.Add(new { text = "83", value = "83" });
            age.Add(new { text = "84", value = "84" });
            age.Add(new { text = "85", value = "85" });
            age.Add(new { text = "86", value = "86" });
            age.Add(new { text = "87", value = "87" }); ;
            age.Add(new { text = "88", value = "88" });
            age.Add(new { text = "89", value = "89" }); ;
            age.Add(new { text = "90", value = "90" });
            age.Add(new { text = "91", value = "91" });
            age.Add(new { text = "92", value = "92" });
            age.Add(new { text = "93", value = "93" });
            age.Add(new { text = "94", value = "94" });
            age.Add(new { text = "95", value = "95" }); ;
            age.Add(new { text = "96", value = "96" });
            age.Add(new { text = "97", value = "97" });
            age.Add(new { text = "98", value = "98" });
            age.Add(new { text = "99", value = "99" });
            age.Add(new { text = "100", value = "100" });

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = age;
            return Ok(responseModel);
        }


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("event-organizer-type")]
        public ActionResult<ResponseModel<IEnumerable<dynamic>>> GetEventOrganizerType()
        {
            ResponseModel<IEnumerable<dynamic>> responseModel = new ResponseModel<IEnumerable<dynamic>>();

            List<dynamic> EventOrganizerTypeItems = new List<dynamic>();

            foreach (object item in Enum.GetValues(typeof(EventOrganizerType)))
            {
                EventOrganizerTypeItems.Add(new { text = ((EventOrganizerType)item).GetDescription(), value = ((EventOrganizerType)item).GetDescription() });
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = EventOrganizerTypeItems;
            return Ok(responseModel);
        }


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("event-type")]
        public ActionResult<ResponseModel<IEnumerable<dynamic>>> GetEventType()
        {
            ResponseModel<IEnumerable<dynamic>> responseModel = new ResponseModel<IEnumerable<dynamic>>();

            List<dynamic> EventTypetems = new List<dynamic>();

            foreach (object item in Enum.GetValues(typeof(EventType)))
            {
                EventTypetems.Add(new { text = ((EventType)item).GetDescription(), value = ((EventType)item).GetDescription() });
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = EventTypetems;
            return Ok(responseModel);
        }


        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("event-status")]
        public ActionResult<ResponseModel<IEnumerable<dynamic>>> GetEventStatus()
        {
            ResponseModel<IEnumerable<dynamic>> responseModel = new ResponseModel<IEnumerable<dynamic>>();

            List<dynamic> EventStatusItems = new List<dynamic>();

            foreach (object item in Enum.GetValues(typeof(EventStatus)))
            {
                EventStatusItems.Add(new { text = ((EventStatus)item).GetDescription(), value = ((EventStatus)item).GetDescription() });
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = EventStatusItems;
            return Ok(responseModel);
        }



        [AllowAnonymous]
        [MapToApiVersion(1)]
        [HttpGet]
        [Route("notification-type")]
        public ActionResult<ResponseModel<IEnumerable<dynamic>>> GetNotificationType()
        {
            ResponseModel<IEnumerable<dynamic>> responseModel = new ResponseModel<IEnumerable<dynamic>>();

            List<dynamic> NotificationTypeItem = new List<dynamic>();

            foreach (object item in Enum.GetValues(typeof(NotificationType)))
            {
                NotificationTypeItem.Add(new { text = ((NotificationType)item).GetDescription(), value = ((NotificationType)item).GetDescription() });
            }

            responseModel.Success = true;
            responseModel.Message = "Success";
            responseModel.Data = NotificationTypeItem;
            return Ok(responseModel);
        }

    }
}
