﻿// Description: Dapper Tutorial | Examples
// Website: http://dapper-tutorial.net/
// More projects: http://www.zzzprojects.com/
// Copyright © ZZZ Projects Inc. 2014 - 2017. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using Z.Dapper.Examples.API.Dapper.Methods;
using Z.Dapper.Plus;

namespace Z.Dapper.Examples.API.DapperPlus.Methods
{
    public partial class Bulk_Merge : Form
    {
        public Bulk_Merge()
        {
            InitializeComponent();
        }

        public void Single(object sender, EventArgs e)
        {
            My.Database.Reset(10000);

            Invoice invoice;

            using (var connection = My.ConnectionFactory())
            {
                invoice = connection.QueryFirst<Invoice>(My.SqlText.Invoice_Select_ByID, new {InvoiceID = 1});
                invoice.Code = "Bulk_Update_0";
            }

            using (var connection = My.ConnectionFactory())
            {
                connection.Open();

                var clock = new Stopwatch();
                clock.Start();

                connection.BulkMerge(invoice);

                clock.Stop();

                My.Result.Show(clock, 1);
            }
        }

        public void Many(object sender, EventArgs e)
        {
            My.Database.Reset(10000);

            List<Invoice> invoices;

            // LOGIC to modify existing entities && Add new entities
            {
                using (var connection = My.ConnectionFactory())
                {
                    invoices = connection.Query<Invoice>(My.SqlText.Invoice_Select).ToList();

                    for (var i = 0; i < invoices.Count; i++)
                    {
                        invoices[i].Code = "Bulk_Update_" + i;
                    }
                }

                for (var i = 0; i < 10000; i++)
                {
                    invoices.Add(new Invoice {Code = "BulkInsert_Many_" + i});
                }
            }

            using (var connection = My.ConnectionFactory())
            {
                connection.Open();

                var clock = new Stopwatch();
                clock.Start();

                connection.BulkMerge(invoices);

                clock.Stop();

                My.Result.Show(clock, invoices.Count);
            }
        }

        public void Relation_OneToOne(object sender, EventArgs e)
        {
            My.Database.Reset(10000);

            var invoices = GetInvoiceWithDetail();

            // LOGIC to modify existing entities && Add new entities
            {
                invoices.ForEach(x =>
                {
                    x.Code += "z";
                    x.Detail.Detail += "z";
                });

                for (var i = 0; i < 10000; i++)
                {
                    var invoice = new Invoice {Code = "BulkInsert_Many_" + i};
                    invoice.Detail = new InvoiceDetail {Detail = "BulkInsert_Many_" + i};
                    invoices.Add(invoice);
                }
            }

            using (var connection = My.ConnectionFactory())
            {
                connection.Open();

                var clock = new Stopwatch();
                clock.Start();

                connection.BulkMerge(invoices)
                    .ThenForEach(x => x.Detail.InvoiceID = x.InvoiceID)
                    .ThenBulkMerge(x => x.Detail);

                clock.Stop();

                My.Result.Show(clock, invoices.Count * 2);
            }
        }

        public void Relation_OneToMany(object sender, EventArgs e)
        {
            My.Database.Reset(10000);

            var invoices = GetInvoiceWithItems();

            // LOGIC to modify existing entities && Add new entities
            {
                invoices.ForEach(x =>
                {
                    x.Code += "z";
                    x.Items.ForEach(y => y.Code += "z");
                });

                for (var i = 0; i < 10000; i++)
                {
                    var invoice = new Invoice {Code = "BulkInsert_Many_" + i};
                    invoice.Items = new List<InvoiceItem>();

                    for (var j = 0; j < 5; j++)
                    {
                        invoice.Items.Add(new InvoiceItem {Code = "BulkInsert_Many_" + i});
                    }

                    invoices.Add(invoice);
                }
            }

            using (var connection = My.ConnectionFactory())
            {
                connection.Open();

                var clock = new Stopwatch();
                clock.Start();

                connection.BulkMerge(invoices)
                    .ThenForEach(x => x.Items.ForEach(y => y.InvoiceID = x.InvoiceID))
                    .ThenBulkMerge(x => x.Items);

                clock.Stop();

                My.Result.Show(clock, invoices.Count + invoices.Sum(x => x.Items.Count));
            }
        }

        private List<Invoice> GetInvoiceWithDetail()
        {
            var sql = My.SqlText.Invoice_Select_WithDetail;

            using (var connection = My.ConnectionFactory())
            {
                connection.Open();

                var invoices = connection.Query<Invoice, InvoiceDetail, Invoice>(
                        sql,
                        (invoice, invoiceDetail) =>
                        {
                            invoice.Detail = invoiceDetail;
                            return invoice;
                        },
                        splitOn: "InvoiceID")
                    .Distinct()
                    .ToList();

                return invoices;
            }
        }

        private List<Invoice> GetInvoiceWithItems()
        {
            var sql = My.SqlText.Invoice_Select_WithItem;

            using (var connection = My.ConnectionFactory())
            {
                connection.Open();

                var invoiceDictionary = new Dictionary<int, Invoice>();

                var invoices = connection.Query<Invoice, InvoiceItem, Invoice>(
                        sql,
                        (invoice, invoiceItem) =>
                        {
                            Invoice invoiceEntry;

                            if (!invoiceDictionary.TryGetValue(invoice.InvoiceID, out invoiceEntry))
                            {
                                invoiceEntry = invoice;
                                invoiceEntry.Items = new List<InvoiceItem>();
                                invoiceDictionary.Add(invoiceEntry.InvoiceID, invoiceEntry);
                            }

                            invoiceEntry.Items.Add(invoiceItem);
                            return invoiceEntry;
                        },
                        splitOn: "InvoiceItemID")
                    .Distinct()
                    .ToList();

                return invoices;
            }
        }
    }
}