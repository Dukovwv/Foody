using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

namespace Foody
{
    [TestFixture]
    public class FoodyTests
    {
        private RestClient client;
        private static string createdFoodId;
        private const string baseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("angel3", "angel123");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password) 
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { username, password });
            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        // All tests here
        [Test, Order(1)]
        public void CreateFood_ShouldReturnCreated() 
        {
            // Arrange
            var food = new
            {
                Name = "TestFood",
                Description = "TestDescription",
                Url = ""
            };

            // Act
            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            var response = client.Execute(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            createdFoodId = json.GetProperty("foodId").GetString() ?? string.Empty;
            Assert.That(createdFoodId, Is.Not.Null.And.Not.Empty, "Food ID should not be null or empty.");
        }

        [Test, Order(2)]
        public void EditFoodTitle_ShouldReturnOk() 
        {
            // Arrange
            var changes = new[]
            {
              new {path = "/name", op = "replace", value = "updated food name" }
            }; 

            // Act
            var request = new RestRequest($"/api/Food/Edit/{createdFoodId}", Method.Patch);
            request.AddJsonBody(changes);
            var response = client.Execute(request);
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)HttpStatusCode.OK));
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllFoods_ShouldReturnList()
        {   
            // Act
            var request = new RestRequest("/api/Food/All", Method.Get);
            var response = client.Execute(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)(HttpStatusCode.OK)));
            var foods = JsonSerializer.Deserialize<List<object>>(response.Content);
            Assert.That(foods, Is.Not.Empty);
        }

        [Test, Order(4)]
        public void DeleteFood_ShouldReturnDeleted() 
        {   
            // Act
            var request = new RestRequest($"/api/Food/Delete/{createdFoodId}", Method.Delete);
            var response = client.Execute(request);
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)(HttpStatusCode.OK)));
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateFood_WithoutReqiredFields_ShouldReturnBadRequest() 
        {
            // Arrange
            var food = new
            {
                Name = "",
                Description = ""
            };

            // Act
            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            var response = client.Execute(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)(HttpStatusCode.BadRequest)));
        }

        [Test, Order(6)]
        public void EditNonExistingFood_ShouldReturnNotFound()
        {
            // Arrange
            string fakeId = "123";
            var changes = new[]
            {
              new {path = "/name", op = "replace", value = "food name" }
            };

            // Act
            var request = new RestRequest($"/api/Food/Edit/{fakeId}", Method.Patch);
            request.AddJsonBody(changes);
            var response = client.Execute(request);
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)(HttpStatusCode.NotFound)));
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("No food revues..."));
        }

        [Test, Order(7)]
        public void DeleteNonExistingFood_ShouldReturn() 
        {
            // Arrange
            string fakeFoodId = "123";

            // Act
            var request = new RestRequest($"/api/Food/Delete/{fakeFoodId}", Method.Delete);
            var response = client.Execute(request);
            //var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)(HttpStatusCode.BadRequest)));
            //Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("Unable to delete this food revue!"));
            Assert.That(response.Content, Does.Contain("Unable to delete this food revue!"));
        }

        [OneTimeTearDown]
        public void Cleanup() 
        {
            client?.Dispose();
        }
    }
}