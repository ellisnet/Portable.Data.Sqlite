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

namespace Portable.Data.Sqlite {
    /// <summary>
    /// Standard interface for platform-specific implementations of database path retrieving classes
    /// </summary>
    public interface IDatabasePath {

        /// <summary>
        /// Gets the platform-specific path to the specified database file
        /// </summary>
        /// <param name="databaseName">The name of the database file</param>
        /// <returns>Path to the specified file (includes filename in path)</returns>
        string GetPath(string databaseName);

    }
}
