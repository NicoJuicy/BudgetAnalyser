﻿using System.Linq;
using BudgetAnalyser.Engine.Budget;
using BudgetAnalyser.Engine.Budget.Data;
using BudgetAnalyser.Engine.UnitTest.TestData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BudgetAnalyser.Engine.UnitTest.Budget
{
    [TestClass]
    public class DtoToBudgetBucketTest
    {
        private BudgetBucket Result { get; set; }
        private BudgetBucketDto TestData => BudgetModelTestData.CreateBudgetBucketDtoTestData1().First(b => b.Code == TestDataConstants.CarMtcBucketCode);

        [TestMethod]
        public void ShouldMapCode()
        {
            Assert.AreEqual(TestData.Code, Result.Code);
        }

        [TestMethod]
        public void ShouldMapDescription()
        {
            Assert.AreEqual(TestData.Description, Result.Description);
        }

        [TestMethod]
        public void ShouldMapType()
        {
            Assert.IsInstanceOfType(Result, typeof(SavedUpForExpenseBucket));
        }

        [TestInitialize]
        public void TestInitialise()
        {
            var subject = new MapperBudgetBucketDtoBudgetBucket(new BudgetBucketFactory());
            Result = subject.ToModel(TestData);
        }
    }
}
