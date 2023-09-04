﻿using AutoMapper;
using Com.DanLiris.Service.DealTracking.Lib.Models;
using Com.DanLiris.Service.DealTracking.Lib.Services;
using Com.DanLiris.Service.DealTracking.Lib.Utilities;
using Com.DanLiris.Service.DealTracking.Lib.Utilities.BaseClass;
using Com.DanLiris.Service.DealTracking.Lib.Utilities.BaseInterface;
using Com.DanLiris.Service.DealTracking.WebApi.Utilities;
using Com.Moonlay.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Com.DanLiris.Service.DealTracking.Test.WebApi.Utilities
{
    public abstract class BaseControllerTest<TController, TModel, TViewModel, IFacade>
           where TController : BaseController<TModel, TViewModel, IFacade>
           where TModel : BaseModel, new()
           where TViewModel : BaseViewModel, IValidatableObject, new()
           where IFacade : class, IBaseFacade<TModel>

    {

        protected virtual TModel Model
        {
            get { return new TModel(); }
        }

        protected virtual TViewModel ViewModel
        {
            get { return new TViewModel(); }
        }

        protected virtual List<TViewModel> Models
        {
            get { return new List<TViewModel>(); }
        }

        protected virtual List<TViewModel> ViewModels
        {
            get { return new List<TViewModel>(); }
        }

        protected ServiceValidationException GetServiceValidationExeption()
        {
            Mock<IServiceProvider> serviceProvider = new Mock<IServiceProvider>();
            List<ValidationResult> validationResults = new List<ValidationResult>();
            System.ComponentModel.DataAnnotations.ValidationContext validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(ViewModel, serviceProvider.Object, null);
            return new ServiceValidationException(validationContext, validationResults);
        }

        protected (Mock<IIdentityService> IdentityService, Mock<IValidateService> ValidateService, Mock<IFacade> Facade, Mock<IMapper> Mapper, Mock<IServiceProvider> ServiceProvider) GetMocks()
        {
            return (IdentityService: new Mock<IIdentityService>(), ValidateService: new Mock<IValidateService>(), Facade: new Mock<IFacade>(), Mapper: new Mock<IMapper>(), ServiceProvider: new Mock<IServiceProvider>());
        }


        protected virtual TController GetController((Mock<IIdentityService> IdentityService, Mock<IValidateService> ValidateService, Mock<IFacade> Facade, Mock<IMapper> Mapper, Mock<IServiceProvider> ServiceProvider) mocks)
        {
            var user = new Mock<ClaimsPrincipal>();
            var claims = new Claim[]
            {
                new Claim("username", "unittestusername")
            };
            user.Setup(u => u.Claims).Returns(claims);
          
             mocks.ServiceProvider.Setup(s => s.GetService(typeof(IHttpClientService))).Returns(new HttpClientTestService());
            //TController controller = (TController)Activator.CreateInstance(typeof(TController), mocks.IdentityService.Object, mocks.ValidateService.Object, mocks.Facade.Object, mocks.Mapper.Object, mocks.ServiceProvider.Object);
            TController controller = (TController)Activator.CreateInstance(typeof(TController), mocks.Mapper.Object, mocks.IdentityService.Object, mocks.ValidateService.Object, mocks.Facade.Object);
            controller.ControllerContext = new ControllerContext()
            {

                HttpContext = new DefaultHttpContext()
                {
                    User = user.Object,

                }
            };
            controller.ControllerContext.HttpContext.Request.Headers["Authorization"] = "Bearer unittesttoken";
            controller.ControllerContext.HttpContext.Request.Path = new PathString("/v1/unit-test");

            return controller;
        }

        protected int GetStatusCode(IActionResult response)
        {
            return (int)response.GetType().GetProperty("StatusCode").GetValue(response, null);
        }

        private int GetStatusCodeGet((Mock<IIdentityService> IdentityService, Mock<IValidateService> ValidateService, Mock<IFacade> Facade, Mock<IMapper> Mapper, Mock<IServiceProvider> ServiceProvider) mocks)
        {
            TController controller = this.GetController(mocks);
            IActionResult response = controller.Get();

            return this.GetStatusCode(response);
        }

        protected ServiceValidationException GetServiceValidationException()
        {
            Mock<IServiceProvider> serviceProvider = new Mock<IServiceProvider>();
            List<ValidationResult> validationResults = new List<ValidationResult>();
            System.ComponentModel.DataAnnotations.ValidationContext validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(this.ViewModel, serviceProvider.Object, null);
            return new ServiceValidationException(validationContext,validationResults);
        }

        [Fact]
        public virtual void Get_WithoutException_ReturnOK()
        {
            var mocks = this.GetMocks();
            Tuple<List<TModel>, int, Dictionary<string, string>, List<string>> result = new Tuple<List<TModel>, int, Dictionary<string, string>, List<string>>(new List<TModel>(), 0, new Dictionary<string, string>(), new List<string>());
           
            mocks.Facade.Setup(f => f.Read(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>())).Returns(result);
            mocks.Mapper.Setup(f => f.Map<List<TViewModel>>(It.IsAny<List<TModel>>())).Returns(this.ViewModels);

            int statusCode = this.GetStatusCodeGet(mocks);
            Assert.Equal((int)HttpStatusCode.OK, statusCode);
        }

        [Fact]
        public void Get_ReadThrowException_ReturnInternalServerError()
        {
            var mocks = this.GetMocks();
            mocks.Facade.Setup(f => f.Read(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception());

            int statusCode = this.GetStatusCodeGet(mocks);
            Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        }

        private async Task<int> GetStatusCodePost((Mock<IIdentityService> IdentityService, Mock<IValidateService> ValidateService, Mock<IFacade> Facade, Mock<IMapper> Mapper, Mock<IServiceProvider> ServiceProvider) mocks)
        {
            TController controller = this.GetController(mocks);
            IActionResult response = await controller.Post(this.ViewModel);

            return this.GetStatusCode(response);
        }

        [Fact]
        public async Task Post_WithoutException_ReturnCreated()
        {
            var mocks = this.GetMocks();
            mocks.ValidateService.Setup(s => s.Validate(It.IsAny<TViewModel>())).Verifiable();
            mocks.Facade.Setup(s => s.Create(It.IsAny<TModel>())).ReturnsAsync(1);

            int statusCode = await this.GetStatusCodePost(mocks);
            Assert.Equal((int)HttpStatusCode.Created, statusCode);
        }

        [Fact]
        public async Task Post_WhenModelState_Invalid_Return_BadRequest()
        {
            var mocks = this.GetMocks();
            mocks.ValidateService.Setup(s => s.Validate(It.IsAny<TViewModel>())).Verifiable();
            mocks.Facade.Setup(s => s.Create(It.IsAny<TModel>())).ReturnsAsync(1);

            TController controller = this.GetController(mocks);
            controller.ModelState.AddModelError("key", "test");

            IActionResult response = await controller.Post(this.ViewModel);

            var statusCode = this.GetStatusCode(response);
            Assert.Equal((int)HttpStatusCode.BadRequest, statusCode);
        }

        [Fact]
        public virtual async Task Post_ThrowServiceValidationExeption_ReturnBadRequest()
        {
            var mocks = GetMocks();
            mocks.ValidateService.Setup(s => s.Validate(It.IsAny<TViewModel>())).Throws(GetServiceValidationExeption());

            int statusCode = await GetStatusCodePost(mocks);
            Assert.Equal((int)HttpStatusCode.BadRequest, statusCode);
        }

        [Fact]
        public virtual async Task Post_ThrowException_ReturnInternalServerError()
        {
            var mocks = GetMocks();
            mocks.ValidateService.Setup(s => s.Validate(It.IsAny<TViewModel>())).Verifiable();
            mocks.Facade.Setup(s => s.Create(It.IsAny<TModel>())).ThrowsAsync(new Exception());

            int statusCode = await GetStatusCodePost(mocks);
            Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        }

        protected async Task<int> GetStatusCodeGetById((Mock<IIdentityService> IdentityService, Mock<IValidateService> ValidateService, Mock<IFacade> Facade, Mock<IMapper> Mapper, Mock<IServiceProvider> ServiceProvider) mocks)
        {
            TController controller = GetController(mocks);
            IActionResult response = await controller.GetById(1);

            return GetStatusCode(response);
        }

        

        [Fact]
        public virtual async Task GetById_NotNullModel_ReturnOK()
        {
            var mocks = GetMocks();


            mocks.Facade.Setup(f => f.ReadById(It.IsAny<long>())).ReturnsAsync(Model);

            int statusCode = await GetStatusCodeGetById(mocks);
            Assert.Equal((int)HttpStatusCode.OK, statusCode);
        }

        [Fact]
        public virtual async Task GetById_NullModel_ReturnNotFound()
        {
            var mocks = this.GetMocks();
            mocks.Mapper.Setup(f => f.Map<TViewModel>(It.IsAny<TModel>())).Returns(this.ViewModel);
            mocks.Facade.Setup(f => f.ReadById(It.IsAny<int>())).ReturnsAsync((TModel)null);

            int statusCode = await this.GetStatusCodeGetById(mocks);
            Assert.Equal((int)HttpStatusCode.NotFound, statusCode);
        }

        [Fact]
        public virtual async Task GetById_ThrowException_ReturnInternalServerError()
        {
            var mocks = this.GetMocks();
            mocks.Facade.Setup(f => f.ReadById(It.IsAny<long>())).ThrowsAsync(new Exception());

            int statusCode = await this.GetStatusCodeGetById(mocks);
            Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        }

       

        private async Task<int> GetStatusCodePut((Mock<IIdentityService> IdentityService, Mock<IValidateService> ValidateService, Mock<IFacade> Facade, Mock<IMapper> Mapper, Mock<IServiceProvider> ServiceProvider) mocks, int id, TViewModel viewModel)
        {
            TController controller = this.GetController(mocks);
            IActionResult response = await controller.Put(id, viewModel);

            return this.GetStatusCode(response);
        }



        [Fact]
        public async System.Threading.Tasks.Task Put_ValidId_ReturnNoContent()
        {
            var mocks = this.GetMocks();
            mocks.ValidateService.Setup(vs => vs.Validate(It.IsAny<TViewModel>())).Verifiable();
           
            mocks.Mapper.Setup(m => m.Map<TModel>(It.IsAny<TViewModel>())).Returns(Model);
            mocks.Facade.Setup(f => f.Update(It.IsAny<int>(), It.IsAny<TModel>())).ReturnsAsync(1);

            int id = (int)Model.Id;
            int statusCode = await this.GetStatusCodePut(mocks, id, ViewModel);
            Assert.Equal((int)HttpStatusCode.NoContent, statusCode);
        }

        [Fact]
        public async System.Threading.Tasks.Task Put_When_ModelState_InValid()
        {
            var mocks = this.GetMocks();
            mocks.ValidateService.Setup(vs => vs.Validate(It.IsAny<TViewModel>())).Verifiable();

            int id = (int)Model.Id;
            TController controller = this.GetController(mocks);
            controller.ModelState.AddModelError("key", "test");

            IActionResult response = await controller.Put(id, ViewModel);

            var result = this.GetStatusCode(response);
            Assert.Equal((int)HttpStatusCode.BadRequest, result);
        }

        [Fact]
        public async System.Threading.Tasks.Task Put_Return_BadRequest()
        {
            var mocks = this.GetMocks();
            mocks.ValidateService.Setup(vs => vs.Validate(It.IsAny<TViewModel>())).Verifiable();
            var id = 1;
            mocks.Mapper.Setup(m => m.Map<TModel>(It.IsAny<TViewModel>())).Returns(Model);
            mocks.Facade.Setup(f => f.Update(It.IsAny<int>(), It.IsAny<TModel>())).ReturnsAsync(1);
           
            int statusCode = await this.GetStatusCodePut(mocks, id, ViewModel);
            Assert.Equal((int)HttpStatusCode.BadRequest, statusCode);
        }

        [Fact]
        public async Task Put_ThrowException_ReturnInternalServerError()
        {
            var mocks = this.GetMocks();
            mocks.ValidateService.Setup(vs => vs.Validate(It.IsAny<TViewModel>())).Verifiable();
            var id = 1;
           
            mocks.Mapper.Setup(m => m.Map<TModel>(It.IsAny<TViewModel>())).Throws(new Exception());
            mocks.Facade.Setup(f => f.Update(It.IsAny<int>(), It.IsAny<TModel>())).ReturnsAsync(1);

            int statusCode = await this.GetStatusCodePut(mocks, id, ViewModel);
            Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        }

        [Fact]
        public async Task Put_Throws_ServiceValidationException()
        {
            var mocks = this.GetMocks();
            mocks.ValidateService.Setup(vs => vs.Validate(It.IsAny<TViewModel>())).Verifiable();
            var id = 1;

            mocks.Mapper.Setup(m => m.Map<TModel>(It.IsAny<TViewModel>())).Throws(GetServiceValidationException());
            mocks.Facade.Setup(f => f.Update(It.IsAny<int>(), It.IsAny<TModel>())).ReturnsAsync(1);

            int statusCode = await this.GetStatusCodePut(mocks, id, ViewModel);
            Assert.Equal((int)HttpStatusCode.BadRequest, statusCode);
        }

        [Fact]
        public async Task Put_Throws_DbUpdateConcurrencyException()
        {
            var mocks = this.GetMocks();
            mocks.ValidateService.Setup(vs => vs.Validate(It.IsAny<TViewModel>())).Verifiable();
            var id = 1;

            Mock<IUpdateEntry> updateEntry = new Mock<IUpdateEntry>();
            List<IUpdateEntry> listData = new List<IUpdateEntry>()
            {
                updateEntry.Object
            };

            IReadOnlyList<IUpdateEntry> readOnlyData = listData.AsReadOnly();


            mocks.Mapper.Setup(m => m.Map<TModel>(It.IsAny<TViewModel>())).Throws(new DbUpdateConcurrencyException("Message",readOnlyData));
            mocks.Facade.Setup(f => f.Update(It.IsAny<int>(), It.IsAny<TModel>())).ReturnsAsync(1);

            int statusCode = await this.GetStatusCodePut(mocks, id, ViewModel);
            Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        }

        private async Task<int> GetStatusCodeDelete((Mock<IIdentityService> IdentityService, Mock<IValidateService> ValidateService, Mock<IFacade> Facade, Mock<IMapper> Mapper, Mock<IServiceProvider> ServiceProvider) mocks)
        {
            TController controller = this.GetController(mocks);
            IActionResult response = await controller.Delete(1);
            return this.GetStatusCode(response);
        }

       

        [Fact]
        public async Task Delete_WithoutException_ReturnNoContent()
        {
            var mocks = this.GetMocks();
            mocks.Facade.Setup(f => f.Delete(It.IsAny<int>())).ReturnsAsync(1);

            int statusCode = await this.GetStatusCodeDelete(mocks);
            Assert.Equal((int)HttpStatusCode.NoContent, statusCode);
        }

        [Fact]
        public async Task Delete_WhenModelState_Invalid_Return_BadRequest()
        {
            var mocks = this.GetMocks();
            mocks.Facade.Setup(f => f.Delete(It.IsAny<int>())).ReturnsAsync(1);

            TController controller = this.GetController(mocks);
            controller.ModelState.AddModelError("key", "test");

            IActionResult response = await controller.Delete(1);
            var result =  this.GetStatusCode(response);
            Assert.Equal((int)HttpStatusCode.BadRequest, result);
        }

        [Fact]
        public async System.Threading.Tasks.Task Delete_ThrowException_ReturnInternalStatusError()
        {
            var mocks = this.GetMocks();
            mocks.Facade.Setup(f => f.Delete(It.IsAny<long>())).ThrowsAsync(new Exception());

            int statusCode = await this.GetStatusCodeDelete(mocks);
            Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        }

    }
}