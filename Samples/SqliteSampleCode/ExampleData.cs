/*
   Copyright 2014 Ellisnet - Jeremy Ellis (jeremy@ellisnet.com)

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections.Generic;

//ADDED TO SAMPLE TO DEMONSTRATE Portable.Data.Sqlite
//(applies to the entire content of this file)
namespace SampleApp.Shared.SqliteSampleCode {
    public static class ExampleData {

        public static List<SampleDataItem> GetData() {

            var result = new List<SampleDataItem>();

            result.Add(new SampleDataItem {
                FirstName = "Jim",
                LastName = "Kirk",
                Birthdate = new DateTime(2233, 3, 22),
                StateAbbreviation = "IA",
                ZipCode = "12345",
                MajorEvents = new List<Tuple<DateTime, string>> {
                            new Tuple<DateTime, string>(new DateTime(2251, 4, 18), "Joined starfleet"),
                            new Tuple<DateTime, string>(new DateTime(2255, 9, 26), "Kobayashi maru, 'nuff said"),
                            new Tuple<DateTime, string>(new DateTime(2258, 5, 19), "Promoted to captain, U.S.S. Enterprise"),
                            new Tuple<DateTime, string>(new DateTime(2270, 7, 22), "Promoted to Addmeerrall")
                        }
            });

            result.Add(new SampleDataItem {
                FirstName = "Joan",
                LastName = "Johnson",
                Birthdate = new DateTime(1964, 8, 22),
                ZipCode = "54321",
                MajorEvents = new List<Tuple<DateTime, string>> {
                            new Tuple<DateTime, string>(new DateTime(1987, 9, 18), "Married Bob")
                        }
            });

            result.Add(new SampleDataItem {
                FirstName = "Bob",
                LastName = "Johnson",
                Birthdate = new DateTime(1961, 5, 23),
                ZipCode = "54321",
                MajorEvents = new List<Tuple<DateTime, string>> {
                            new Tuple<DateTime, string>(new DateTime(1987, 9, 18), "Married Joan")
                        }
            });

            result.Add(new SampleDataItem {
                FirstName = "Ned",
                LastName = "Johnson",
                Birthdate = new DateTime(1992, 12, 18),
                ZipCode = "54321",
                MajorEvents = new List<Tuple<DateTime, string>> {
                            new Tuple<DateTime, string>(new DateTime(2010, 9, 12), "Off to college")
                        }
            });

            result.Add(new SampleDataItem {
                FirstName = "Ben",
                LastName = "Jacobs",
                Birthdate = new DateTime(1949, 3, 16),
                ZipCode = "54806",
                MajorEvents = new List<Tuple<DateTime, string>> {
                            new Tuple<DateTime, string>(new DateTime(1967, 6, 18), "Drafted into army")
                        }
            });

            result.Add(new SampleDataItem {
                FirstName = "John",
                LastName = "Thompson",
                Birthdate = new DateTime(1972, 11, 16),
                ZipCode = "55112",
                MajorEvents = new List<Tuple<DateTime, string>> {
                            new Tuple<DateTime, string>(new DateTime(1984, 1, 22), "Apple commercial at SuperBowl")
                        }
            });

            return result;
        }

    }
}