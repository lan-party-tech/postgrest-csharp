﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Supabase.Postgrest;
using PostgrestTests.Models;
using static Supabase.Postgrest.Constants;

namespace PostgrestTests
{
    [TestClass]
    public class ReferenceTests
    {
        private const string BaseUrl = "http://localhost:54321/rest/v1";

        [TestMethod("Reference: Returns linked models on a root model.")]
        public async Task TestReferenceReturnsLinkedModels()
        {
            var client = new Client(BaseUrl);

            var movies = await client.Table<Movie>()
                .Order(x => x.Id, Ordering.Ascending)
                .Get();

            Assert.IsTrue(movies.Models.Count > 0);

            var first = movies.Models.First(x => x.Name!.Contains("Top Gun"));
            Assert.IsTrue(first.People.Count > 0);

            var person = first.People.First();
            Assert.IsNotNull(person.Profile);

            var person2 = await client.Table<Person>()
                .Filter("first_name", Operator.Equals, "Bob")
                .Single();

            Assert.IsNotNull(person2?.Profile);
            Assert.IsTrue(person2.Profile.Email!.Contains("bob"));

            var byEmail = await client.Table<Person>()
                .Order(x => x.CreatedAt, Ordering.Ascending)
                .Filter("profile.email", Operator.Equals, "bob.saggett@supabase.io")
                .Single();

            Assert.IsNotNull(byEmail);

            var product = await client.Table<Product>()
                .Filter("id", Operator.Equals, "8b8e89a0-63c7-4917-8dc1-7797dc0285f1")
                .Single();

            Assert.IsNotNull(product);
            Assert.AreEqual("product 1", product.Name);
            
            Assert.IsNotNull(product.Category);
            Assert.AreEqual("999e4b26-91a8-4ea4-af2c-77a3540f7843", product.Category.Id.ToString());
            Assert.AreEqual("category 1", product.Category.Name);
            
            var products = await client.Table<Product>()
                .Get();
            Assert.IsNotNull(products.Models);
            Assert.IsTrue(products.Models.Count == 3);
            
            var productFiltered = products.Models.Find(x => x.Id.ToString() == "8b8e89a0-63c7-4917-8dc1-7797dc0285f1");
            Assert.AreEqual("999e4b26-91a8-4ea4-af2c-77a3540f7843", productFiltered?.Category?.Id.ToString());
            Assert.AreEqual("category 1", productFiltered?.Category?.Name);
        }

        [TestMethod("Reference: Can create linked records.")]
        public async Task TestReferenceCreateLinked()
        {
            var client = new Client(BaseUrl);

            var movie = new Movie { Name = "Supabase in Action" };
            var movieResponse = await client.Table<Movie>().Insert(movie);
            var movieModel = movieResponse.Model;
            Assert.IsNotNull(movieModel);

            var people = new List<Person>
            {
                new() { FirstName = "John", LastName = "Doe" },
                new() { FirstName = "Jane", LastName = "Buck" }
            };

            var peopleModels = await client.Table<Person>().Insert(people);
            Assert.IsTrue(peopleModels.Models.Count == 2);

            var profiles = new List<Profile>
            {
                new() { PersonId = peopleModels.Models[0].Id, Email = "john.doe@email.com" },
                new() { PersonId = peopleModels.Models[1].Id, Email = "jane.buck@email.com" },
            };
            var profileModels = await client.Table<Profile>().Insert(profiles);
            Assert.IsTrue(profileModels.Models.Count == 2);

            var moviePeople = new List<MoviePerson>
            {
                new() { PersonId = peopleModels.Models[0].Id, MovieId = movieModel.Id },
                new() { PersonId = peopleModels.Models[1].Id, MovieId = movieModel.Id }
            };

            var moviePeopleModels = await client.Table<MoviePerson>().Insert(moviePeople);
            Assert.IsTrue(moviePeopleModels.Models.Count == 2);

            var response = await client.Table<Movie>().Where(x => x.Id == movieModel.Id).Get();
            var testRelations = response.Model!;
            Assert.IsNotNull(testRelations);
            Assert.IsNotNull(testRelations.People.Find(x => x.Id == peopleModels.Models[0].Id));
            Assert.IsNotNull(testRelations.People.Find(x => x.Id == peopleModels.Models[1].Id));
            Assert.IsNotNull(testRelations.People[0].Movies.Find(x => x.Id == movieModel.Id));
            Assert.IsNotNull(testRelations.People[1].Movies.Find(x => x.Id == movieModel.Id));
            Assert.AreEqual(testRelations.People[0].Profile!.PersonId, profileModels.Models[0].PersonId);
            Assert.AreEqual(testRelations.People[1].Profile!.PersonId, profileModels.Models[1].PersonId);

            // Circular references should return 1 layer of references, otherwise null.
            Assert.IsTrue(testRelations.People[0].Movies[0].People.Count == 0);
            Assert.IsNotNull(testRelations.People[0].Profile!.Person);
        }

        [TestMethod("Reference: Table can reference the same foreign table multiple times.")]
        public async Task TestModelCanReferenceSameForeignTableMultipleTimes()
        {
            var client = new Client(BaseUrl);

            var response = await client.Table<ForeignKeyTestModel>().Get();

            Assert.IsTrue(response.Models.Count > 0);
            Assert.IsInstanceOfType(response.Model!.MovieFK1, typeof(Movie));
            Assert.IsInstanceOfType(response.Model!.MovieFK2, typeof(Movie));
            Assert.IsInstanceOfType(response.Model!.RandomPersonFK, typeof(Person));
        }

        [TestMethod("Reference: Table can reference a nested model with the same foreign table multiple times.")]
        public async Task TestModelCanReferenceNestedModelWithSameForeignTableMultipleTimes()
        {
            var client = new Client(BaseUrl);

            var response = await client.Table<NestedForeignKeyTestModel>().Get();

            Assert.IsTrue(response.Models.Count > 0);
            Assert.IsInstanceOfType(response.Model!.User, typeof(User));
            Assert.IsInstanceOfType(response.Model!.FKTestModel, typeof(ForeignKeyTestModel));
        }

        [TestMethod("Reference: Model inserts and updates (ignoring reference properties) when Reference is not null.")]
        public async Task TestModelInsertsAndUpdatesWhenReferenceIsSpecified()
        {
            var client = new Client(BaseUrl);

            var newModel = new Movie()
            {
                Name = "The Blue Eyed Samurai (Movie)",
                Status = MovieStatus.OffDisplay,
                People =
                [
                    new Person { FirstName = "Maya", LastName = "Erskine" },
                    new Person { FirstName = "Masi", LastName = "Oka" }
                ]
            };

            var insertedModel = await client.Table<Movie>().Insert(newModel);

            Assert.IsNotNull(insertedModel.Model);
            Assert.IsNotNull(insertedModel.Model.Name);
            Assert.AreEqual(MovieStatus.OffDisplay, insertedModel.Model.Status);
            
            insertedModel.Model.Status = MovieStatus.OnDisplay;

            var updatedModel = await insertedModel.Model.Update<Movie>();
            
            Assert.IsNotNull(updatedModel.Model);
            Assert.AreEqual(MovieStatus.OnDisplay, updatedModel.Model.Status);
        }
    }
}