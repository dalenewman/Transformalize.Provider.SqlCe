﻿#region license
// Transformalize
// Configurable Extract, Transform, and Load
// Copyright 2013-2017 Dale Newman
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//       http://www.apache.org/licenses/LICENSE-2.0
//   
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using Autofac;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transformalize.Configuration;
using Transformalize.Containers.Autofac;
using Transformalize.Contracts;
using Transformalize.Logging;
using Transformalize.Providers.SqlCe;
using Transformalize.Providers.SqlCe.Autofac;
using Transformalize.Providers.SqlServer;
using Transformalize.Providers.SqlServer.Autofac;
using Transformalize.Transforms.Jint.Autofac;

namespace IntegrationTests {

   [TestClass]
   public class NorthWindIntegrationSqlCe {

      public string TestFile { get; set; } = @"Files\NorthWindSqlServerToSqlCe.xml";

      public Connection InputConnection { get; set; } = new Connection {
         Name = "input",
         Provider = "sqlserver",
         Server = "localhost",
         Database = "Northwind"
      };

      public Connection OutputConnection { get; set; } = new Connection {
         Name = "output",
         Provider = "sqlce",
         File = @"c:\temp\northwind.sdf"
      };

      [TestMethod]
      //[Ignore]
      public void SqlCe_Integration() {

         var logger = new DebugLogger(LogLevel.Debug);

         // CORRECT DATA AND INITIAL LOAD
         using (var cn = new SqlServerConnectionFactory(InputConnection).GetConnection()) {
            cn.Open();
            Assert.AreEqual(3, cn.Execute(@"
                    UPDATE [Order Details] SET UnitPrice = 14.40, Quantity = 42 WHERE OrderId = 10253 AND ProductId = 39;
                    UPDATE Orders SET CustomerID = 'CHOPS', Freight = 22.98 WHERE OrderId = 10254;
                    UPDATE Customers SET ContactName = 'Palle Ibsen' WHERE CustomerID = 'VAFFE';
                "));
         }

         // RUN INIT AND TEST
         using (var outer = new ConfigurationContainer(new JintTransformModule()).CreateScope(TestFile + "?Mode=init", logger)) {
            var process = outer.Resolve<Process>();
            using (var inner = new Container(new JintTransformModule(), new SqlServerModule(), new SqlCeModule()).CreateScope(process, new DebugLogger(LogLevel.Debug))) {
               var controller = inner.Resolve<IProcessController>();
               controller.Execute();
            }
         }

         using (var cn = new SqlCeConnectionFactory(OutputConnection).GetConnection()) {
            cn.Open();
            Assert.AreEqual(2155, cn.ExecuteScalar<int>("SELECT TOP 1 Inserts FROM NorthWindControl WHERE Entity = 'Order Details' AND BatchId = 1;"));
            // Assert.AreEqual(2155, cn.ExecuteScalar<int>("SELECT COUNT(*) FROM NorthWindFlat;"));
         }

         // FIRST DELTA, NO CHANGES
         using (var outer = new ConfigurationContainer(new JintTransformModule()).CreateScope(TestFile, logger)) {
            var process = outer.Resolve<Process>();
            using (var inner = new Container(new JintTransformModule(), new SqlServerModule(), new SqlCeModule()).CreateScope(process, new DebugLogger(LogLevel.Debug))) {
               var controller = inner.Resolve<IProcessController>();
               controller.Execute();
            }
         }

         using (var cn = new SqlCeConnectionFactory(OutputConnection).GetConnection()) {
            cn.Open();
            Assert.AreEqual(2155, cn.ExecuteScalar<int>("SELECT COUNT(*) FROM [NorthWindOrder DetailsTable];"));
            Assert.AreEqual(0, cn.ExecuteScalar<int>("SELECT TOP 1 Inserts+Updates+Deletes FROM NorthWindControl WHERE Entity = 'Order Details' AND BatchId = 9;"));
            Assert.AreEqual(2155, cn.ExecuteScalar<int>("SELECT COUNT(*) FROM NorthWindFlat;"));
         }

         // CHANGE 2 FIELDS IN 1 RECORD IN MASTER TABLE THAT WILL CAUSE CALCULATED FIELD TO BE UPDATED TOO 
         using (var cn = new SqlServerConnectionFactory(InputConnection).GetConnection()) {
            cn.Open();
            const string sql = @"UPDATE [Order Details] SET UnitPrice = 15, Quantity = 40 WHERE OrderId = 10253 AND ProductId = 39;";
            Assert.AreEqual(1, cn.Execute(sql));
         }

         // RUN AND CHECK
         using (var outer = new ConfigurationContainer(new JintTransformModule()).CreateScope(TestFile, logger)) {
            var process = outer.Resolve<Process>();
            using (var inner = new Container(new JintTransformModule(), new SqlServerModule(), new SqlCeModule()).CreateScope(process, new DebugLogger(LogLevel.Debug))) {
               var controller = inner.Resolve<IProcessController>();
               controller.Execute();
            }
         }


         using (var cn = new SqlCeConnectionFactory(OutputConnection).GetConnection()) {
            cn.Open();
            Assert.AreEqual(1, cn.ExecuteScalar<int>("SELECT TOP 1 Updates FROM NorthWindControl WHERE Entity = 'Order Details' AND BatchId = 17;"));

            Assert.AreEqual(15.0M, cn.ExecuteScalar<decimal>("SELECT OrderDetailsUnitPrice FROM NorthWindFlat WHERE OrderDetailsOrderId= 10253 AND OrderDetailsProductId = 39;"));
            Assert.AreEqual(40, cn.ExecuteScalar<int>("SELECT OrderDetailsQuantity FROM NorthWindFlat WHERE OrderDetailsOrderId= 10253 AND OrderDetailsProductId = 39;"));
            Assert.AreEqual(15.0 * 40, cn.ExecuteScalar<int>("SELECT OrderDetailsExtendedPrice FROM NorthWindFlat WHERE OrderDetailsOrderId= 10253 AND OrderDetailsProductId = 39;"));

         }

         // CHANGE 1 RECORD'S CUSTOMERID AND FREIGHT ON ORDERS TABLE
         using (var cn = new SqlServerConnectionFactory(InputConnection).GetConnection()) {
            cn.Open();
            Assert.AreEqual(1, cn.Execute("UPDATE Orders SET CustomerID = 'VICTE', Freight = 20.11 WHERE OrderId = 10254;"));
         }

         using (var outer = new ConfigurationContainer(new JintTransformModule()).CreateScope(TestFile, logger)) {
            var process = outer.Resolve<Process>();
            using (var inner = new Container(new JintTransformModule(), new SqlServerModule(), new SqlCeModule()).CreateScope(process, new DebugLogger(LogLevel.Debug))) {
               var controller = inner.Resolve<IProcessController>();
               controller.Execute();
            }
         }

         using (var cn = new SqlCeConnectionFactory(OutputConnection).GetConnection()) {
            cn.Open();
            Assert.AreEqual(1, cn.ExecuteScalar<int>("SELECT Updates FROM NorthWindControl WHERE Entity = 'Orders' AND BatchId = 26;"));

            Assert.AreEqual("VICTE", cn.ExecuteScalar<string>("SELECT OrdersCustomerId FROM NorthWindFlat WHERE OrderDetailsOrderId= 10254;"));
            Assert.AreEqual(20.11M, cn.ExecuteScalar<decimal>("SELECT OrdersFreight FROM NorthWindFlat WHERE OrderDetailsOrderId= 10254;"));
            Assert.AreEqual(26, cn.ExecuteScalar<int>("SELECT TflBatchId FROM NorthWindFlat WHERE OrderDetailsOrderId= 10254;"));
         }

         // CHANGE A CUSTOMER'S CONTACT NAME FROM Palle Ibsen TO Paul Ibsen
         using (var cn = new SqlServerConnectionFactory(InputConnection).GetConnection()) {
            cn.Open();
            Assert.AreEqual(1, cn.Execute("UPDATE Customers SET ContactName = 'Paul Ibsen' WHERE CustomerID = 'VAFFE';"));
         }

         using (var outer = new ConfigurationContainer(new JintTransformModule()).CreateScope(TestFile, logger)) {
            var process = outer.Resolve<Process>();
            using (var inner = new Container(new JintTransformModule(), new SqlServerModule(), new SqlCeModule()).CreateScope(process, new DebugLogger(LogLevel.Debug))) {
               var controller = inner.Resolve<IProcessController>();
               controller.Execute();
            }
         }

         using (var cn = new SqlCeConnectionFactory(OutputConnection).GetConnection()) {
            cn.Open();
            Assert.AreEqual(1, cn.ExecuteScalar<int>("SELECT Updates FROM NorthWindControl WHERE Entity = 'Customers' AND BatchId = 35;"));

            Assert.AreEqual("Paul Ibsen", cn.ExecuteScalar<string>("SELECT DISTINCT CustomersContactName FROM NorthWindFlat WHERE OrdersCustomerID = 'VAFFE';"));
            Assert.AreEqual(35, cn.ExecuteScalar<int>("SELECT DISTINCT TflBatchId FROM NorthWindFlat WHERE OrdersCustomerID = 'VAFFE';"), "The TflBatchId should be updated on the master to indicate a change has occured.");

         }

      }
   }
}
