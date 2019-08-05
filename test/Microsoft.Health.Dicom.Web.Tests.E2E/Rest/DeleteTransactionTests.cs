﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class DeleteTransactionTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        public DeleteTransactionTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            Client = new DicomWebClient(fixture.HttpClient);
        }

        protected DicomWebClient Client { get; set; }

        [Theory]
        [InlineData("studies")]
        [InlineData("studies/")]
        [InlineData("studies/invalidStudyId")]
        [InlineData("studies/invalidStudyId/series")]
        [InlineData("studies/invalidStudyId/series/")]
        [InlineData("studies/invalidStudyId/series/invalidSeriesId")]
        [InlineData("studies/invalidStudyId/series/invalidSeriesId/instances")]
        [InlineData("studies/invalidStudyId/series/invalidSeriesId/instances/")]
        [InlineData("studies/invalidStudyId/series/invalidSeriesId/instances/invalidInstanceId")]
        [InlineData("studies//series/invalidSeriesId")]
        [InlineData("studies/invalidStudyId/series//instances/invalidInstanceId")]
        public async Task GivenInvalidUID_WhenDeleting_TheServerShouldReturnNotFound(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url);

            using (HttpResponseMessage response = await Client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Theory]
        [InlineData("studies/&^%")]
        [InlineData("studies/123/series/&^%")]
        [InlineData("studies/123/series/456/instances/&^%")]
        public async Task GivenBadUIDFormats_WhenDeleting_TheServerShouldReturnBadRequest(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url);

            using (HttpResponseMessage response = await Client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async void GivenValidStudyId_WhenDeletingStudy_TheServerShouldReturnOK()
        {
            // Add 10 series with 10 instances each to a single study
            var files = new DicomFile[100];
            var studyInstanceUID = Guid.NewGuid().ToString();
            for (int i = 0; i < 10; i++)
            {
                var seriesUID = Guid.NewGuid().ToString();
                for (int j = 0; j < 10; j++)
                {
                    files[i + (j * 10)] = Samples.CreateRandomDicomFile(studyInstanceUID: studyInstanceUID, seriesInstanceUID: seriesUID);
                }
            }

            await Client.PostAsync(files);

            // Send the delete request
            HttpStatusCode result = await Client.DeleteAsync(studyInstanceUID);
            Assert.Equal(HttpStatusCode.OK, result);
        }

        [Fact]
        public async void GivenValidSeriesId_WhenDeletingSeries_TheServerShouldReturnOK()
        {
            // Store series with 10 instances
            var studyInstanceUID = Guid.NewGuid().ToString();
            var seriesUID = Guid.NewGuid().ToString();
            var files = new DicomFile[10];
            for (int i = 0; i < 10; i++)
            {
                files[i] = Samples.CreateRandomDicomFile(studyInstanceUID: studyInstanceUID, seriesInstanceUID: seriesUID);
            }

            await Client.PostAsync(files);

            // Send the delete request
            HttpStatusCode result = await Client.DeleteAsync(studyInstanceUID, seriesUID);
            Assert.Equal(HttpStatusCode.OK, result);
        }

        [Fact]
        public async void GivenValidInstanceId_WhenDeletingInstance_TheServerShouldReturnOK()
        {
            // Create and upload file
            var studyInstanceUID = Guid.NewGuid().ToString();
            var seriesUID = Guid.NewGuid().ToString();
            var instanceUID = Guid.NewGuid().ToString();
            DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID: studyInstanceUID, seriesInstanceUID: seriesUID, sopInstanceUID: instanceUID);
            await Client.PostAsync(new[] { dicomFile });

            // Send the delete request
            HttpStatusCode result = await Client.DeleteAsync(studyInstanceUID, seriesUID, instanceUID);
            Assert.Equal(HttpStatusCode.OK, result);
        }
    }
}