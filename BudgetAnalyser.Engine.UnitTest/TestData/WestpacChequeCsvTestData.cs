﻿using System.Collections.Generic;

namespace BudgetAnalyser.Engine.UnitTest.TestData
{
    public static class WestpacChequeCsvTestData
    {
        internal static string FirstTwoLines1()
        {
            return "Date,Amount,Other Party,Description,Reference,Particulars,Analysis Code\r\n20/07/2020,-12.50,\"Brew On Quay\",\"EFTPOS TRANSACTION\",\"20-16:10-941\",\"************\",\"7786 30941\"";
        }

        public static IEnumerable<string> BadTestData1()
        {
            return new List<string>
            {
                @"Date,Amount,Other Party,Description,Reference,Particulars,Analysis Code",
                @"20/07/2020,25.26,""Acme Inc"",""DIRECT CREDIT"",""IFT012345667"",""IFT01234566"",""00444565652",
                @"20/07/2020,-12.50,""Java On Quay"",""EFTPOS TRANSACTION"",""20-16:10-941"",""************"",""7722 43533",
                @"20/07/2020,-12.50,""Java On Quay"",""EFTPOS TRANSACTION"",""20-12:08-200"",""************"",""7722 34422",
                @"21/07/2020 -11.22,""The Stick       17"",""DEBIT"",,""************"",""7722",  // Commas missing from this row
                @"23/07/2020,109.16,""From 0296-0421989-00"",""DIRECT CREDIT"",""17:12-20714"",""Rego"",""Abc222",
                @"23/07/2020,-24.00,""Java On Quay"",""EFTPOS TRANSACTION"",""23-12:53-978"",""************"",""7722 24323",
                @"24/07/2020,-25.00,""Java On Quay"",""EFTPOS TRANSACTION"",""24-18:29-576"",""************"",""7722 34343",
            };
        }

        public static IEnumerable<string> TestData1()
        {
            return new List<string>
            {
                @"Date,Amount,Other Party,Description,Reference,Particulars,Analysis Code",
                @"20/07/2020,25.26,""Acme Inc"",""DIRECT CREDIT"",""IFT012345667"",""IFT01234566"",""00444565652",
                @"20/07/2020,-12.50,""Java On Quay"",""EFTPOS TRANSACTION"",""20-16:10-941"",""************"",""7722 43533",
                @"20/07/2020,-12.50,""Java On Quay"",""EFTPOS TRANSACTION"",""20-12:08-200"",""************"",""7722 34422",
                @"21/07/2020,-11.22,""The Stick       17"",""DEBIT"",,""************"",""7722",
                @"23/07/2020,109.16,""From 0296-0421989-00"",""DIRECT CREDIT"",""17:12-20714"",""Rego"",""Abc222",
                @"23/07/2020,-24.00,""Java On Quay"",""EFTPOS TRANSACTION"",""23-12:53-978"",""************"",""7722 24323",
                @"24/07/2020,-25.00,""Java On Quay"",""EFTPOS TRANSACTION"",""24-18:29-576"",""************"",""7722 34343",
            };
        }

        public static IEnumerable<string> TestData2()
        {
            return new List<string>
            {
                @"Date,Amount,Other Party,Description,Reference,Particulars,Analysis Code",
                @"20/07/2020,25.26,""Acme Inc"",""DIRECT CREDIT"",""IFT012345667"",""IFT01234566"",""00444565652",
                @"20/07/2020,-12.50,""Java On Quay"",""EFTPOS TRANSACTION"",""20-16:10-941"",""************"",""7722 43533",
                @"20/07/2020,-12.50,""Java On Quay"",""EFTPOS TRANSACTION"",""20-12:08-200"",""************"",""7722 34422"",""more"",""data"",""here",
                @"21/07/2020,-11.22,""The Stick       17"",""DEBIT"",,""************"",""7722",
                @"23/07/2020,109.16,""From 0296-0421989-00"",""DIRECT CREDIT"",""17:12-20714"",""Rego"",""Abc222",
                @"23/07/2020,-24.00,""Java On Quay"",""EFTPOS TRANSACTION"",""23-12:53-978"",""************"",""7722 24323",
                @"24/07/2020,-25.00,""Java On Quay"",""EFTPOS TRANSACTION"",""24-18:29-576"",""************"",""7722 34343",
            };
        }
    }
}
