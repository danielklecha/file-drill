using FileDrill.Models;
using FileDrill.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.CommandLine.Invocation;
using System.CommandLine;

namespace FileDrill.Commands;
internal class ConfigSeedCommand() : Command("seed", "Fills \"Schemas\" section with sample data")
{
    public new class Handler(
        ILogger<Handler> logger,
        IOptions<WritableOptions> options,
        IOptionsSync<WritableOptions> optionsSync) : ICommandHandler
    {
        public int Invoke(InvocationContext context) => 0;

        public async Task<int> InvokeAsync(InvocationContext context)
        {
            WritableOptions optionsValue = options.Value;
            optionsValue.Schemas ??= [];
            foreach (var sample in GetSamples())
                optionsValue.Schemas.TryAdd(sample.Key, sample.Value);
            await optionsSync.SyncAsync(optionsValue);
            await optionsSync.SaveAsync();
            logger.LogInformation("Options were updated");
            return 0;
        }

        private static Dictionary<string, SchemaOptions> GetSamples()
        {
            return new()
            {
                ["Invoice"] = new()
                {
                    Description = "an invoice, also known as a bill or sales",
                    Fields = new()
                    {
                        ["invoice number"] = new() { Description = "The number of the invoice" },
                        ["date emission"] = new() { Description = "The date of the invoice", Type = FieldType.DateTime },
                        ["due date"] = new() { Description = "The date of the invoice", Type = FieldType.DateTime },
                        ["customer name"] = new() { Description = "The customer's name mentioned on the invoice" },
                        ["customer address"] = new() { Description = "The customer's address mentioned on the invoice" },
                        ["vendor name"] = new() { Description = "The vendor's name mentioned on the invoice" },
                        ["vendor address"] = new() { Description = "The vendor's address mentioned on the invoice" },
                        ["total amount VAT excluded"] = new() { Description = "The total amount of the invoice, excluding taxes", Type = FieldType.Decimal },
                        ["total amount VAT included"] = new() { Description = "The total amount of the invoice, including taxes", Type = FieldType.Decimal },
                        ["vat percentage"] = new() { Description = "The VAT percentage applied", Type = FieldType.Decimal },
                        ["vat amount"] = new() { Description = "The VAT amount to be paid", Type = FieldType.Decimal },
                        ["currency"] = new() { Description = "Currency used in this invoice" }
                    }
                },
                ["Resume"] = new()
                {
                    Description = "a resume, also known as a curriculum vitae",
                    Fields = new()
                    {
                        ["first name"] = new() { Description = "The first name of the person mentioned on the resume" },
                        ["last name"] = new() { Description = "The last name of the person mentioned on the resume" },
                        ["email"] = new() { Description = "The email of the person mentioned on the resume" },
                        ["phone number"] = new() { Description = "The phone number of the person mentioned on the resume" },
                        ["birth date"] = new() { Description = "The birth date of the person mentioned on the resume", Type = FieldType.DateTime },
                        ["age"] = new() { Description = "The age of the person mentioned on the resume", Type = FieldType.Decimal },
                        ["years of experience"] = new() { Description = "The total number of years of work of the person mentioned on the resume", Type = FieldType.Decimal },
                        ["technologies"] = new() { Description = "The technologies used on previous experiences of the person mentioned on the resume" },
                        ["languages"] = new() { Description = "The languages the person mentioned on the resume can talk with the level of each of them" }
                    }
                },
                ["Receipt"] = new()
                {
                    Description = "a receipt, also known as an order acknowledgment and sales receipt",
                    Fields = new()
                    {
                        ["po number"] = new() { Description = "The number of the purchase order" },
                        ["date emission"] = new() { Description = "The date of the purchase order", Type = FieldType.DateTime },
                        ["buyer name"] = new() { Description = "The buyer's name mentioned on the purchase order" },
                        ["buyer address"] = new() { Description = "The buyer's address mentioned on the purchase order" },
                        ["vendor name"] = new() { Description = "The vendor's name mentioned on the purchase order" },
                        ["vendor address"] = new() { Description = "The vendor's address mentioned on the purchase order" },
                        ["total VAT excluded"] = new() { Description = "The total price without taxes of the purchase order", Type = FieldType.Decimal },
                        ["total VAT included"] = new() { Description = "The total price with taxes included of the purchase order", Type = FieldType.Decimal },
                        ["vat percentage"] = new() { Description = "The VAT percentage applied", Type = FieldType.Decimal },
                        ["vat amount"] = new() { Description = "The VAT amount to be paid", Type = FieldType.Decimal },
                        ["currency"] = new() { Description = "Currency used in this receipt" }
                    }
                },
                ["Bank details"] = new()
                {
                    Description = "a bank account details, also known as bank account information",
                    Fields = new()
                    {
                        ["bank name"] = new() { Description = "The name of the bank" },
                        ["iban"] = new() { Description = "The international bank deposit number, usually called IBAN" },
                        ["account number"] = new() { Description = "The account number" },
                        ["bank number"] = new() { Description = "The bank number" },
                        ["bic code"] = new() { Description = "The bank identifier code, usually called BIC" },
                        ["bank address"] = new() { Description = "The postal address of the bank" },
                        ["owner name"] = new() { Description = "The owner's name" },
                        ["owner address"] = new() { Description = "The owner's address" },
                        ["currency"] = new() { Description = "The currency of the bank account" }
                    }
                },
                ["Driver license"] = new()
                {
                    Description = "a driver's license, also known as driving permit and driving licence",
                    Fields = new()
                    {
                        ["license number"] = new() { Description = "The driver license number" },
                        ["country"] = new() { Description = "The three letters of the country of the driver license" },
                        ["first name"] = new() { Description = "The first name of the driver license owner" },
                        ["second name"] = new() { Description = "The second name of the driver license owner" },
                        ["last name"] = new() { Description = "The last name of the driver license owner" },
                        ["birth date"] = new() { Description = "The birth date value of the driver license owner", Type = FieldType.DateTime }
                    }
                },
                ["Id Card"] = new()
                {
                    Description = "an identity card, also known as a personal identification card",
                    Fields = new()
                    {
                        ["card number"] = new() { Description = "The id card number" },
                        ["country"] = new() { Description = "The three letters of the country of the id card" },
                        ["first name"] = new() { Description = "The first name of the id card owner" },
                        ["second name"] = new() { Description = "The second name of the id card owner" },
                        ["last name"] = new() { Description = "The last name of the id card owner" },
                        ["birth date"] = new() { Description = "The birth date of the id card owner", Type = FieldType.DateTime }
                    }
                },
                ["Passport"] = new()
                {
                    Description = "a passport",
                    Fields = new()
                    {
                        ["passport number"] = new() { Description = "The passport number" },
                        ["country"] = new() { Description = "The country mentioned in the passport" },
                        ["first name"] = new() { Description = "The first name of passport owner" },
                        ["second name"] = new() { Description = "The second name of passport owner" },
                        ["last name"] = new() { Description = "The last name of passport owner" },
                        ["birth date"] = new() { Description = "The birth date of the passport owner", Type = FieldType.DateTime }
                    }
                },
                ["Payment card"] = new()
                {
                    Description = "a credit card, also known as a transaction card and bank card",
                    Fields = new()
                    {
                        ["card number"] = new() { Description = "The payment card number" },
                        ["expiration date"] = new() { Description = "The expiration date of the payment card", Type = FieldType.DateTime },
                        ["cvv"] = new() { Description = "The card verification code or card verification value" },
                        ["owner name"] = new() { Description = "The name of the owner of the card" }
                    }
                },
                ["Payroll statement"] = new()
                {
                    Description = "a payroll statement, also known as pay stub and pay slip",
                    Fields = new()
                    {
                        ["employee first name"] = new() { Description = "The first name of the employee" },
                        ["employee last name"] = new() { Description = "The last name of the employee" },
                        ["employee SSN"] = new() { Description = "The employee's social security number" },
                        ["net salary"] = new() { Description = "The amount of net salary", Type = FieldType.Decimal },
                        ["gross salary"] = new() { Description = "The amount of gross salary", Type = FieldType.Decimal },
                        ["pay date"] = new() { Description = "The date of wage payment", Type = FieldType.DateTime },
                        ["period beginning"] = new() { Description = "Pay stub start date", Type = FieldType.DateTime },
                        ["period ending"] = new() { Description = "Pay stub end date", Type = FieldType.DateTime }
                    }
                },
                ["Purchase order"] = new()
                {
                    Description = "a purchase order, also known as a procurement order",
                    Fields = new()
                    {
                        ["po number"] = new() { Description = "The number of the purchase order" },
                        ["date emission"] = new() { Description = "The date of the purchase order" },
                        ["buyer name"] = new() { Description = "The buyer's name mentioned on the purchase order" },
                        ["buyer address"] = new() { Description = "The buyer's address mentioned on the purchase order" },
                        ["vendor name"] = new() { Description = "The vendor's name mentioned on the purchase order" },
                        ["vendor address"] = new() { Description = "The vendor's address mentioned on the purchase order" },
                        ["total VAT excluded"] = new() { Description = "The total price without taxes of the purchase order", Type = FieldType.Decimal },
                        ["total VAT included"] = new() { Description = "The total price with taxes included of the purchase order", Type = FieldType.Decimal },
                        ["vat percentage"] = new() { Description = "The VAT percentage applied", Type = FieldType.Decimal },
                        ["vat amount"] = new() { Description = "The VAT amount to be paid", Type = FieldType.Decimal },
                        ["currency"] = new() { Description = "Currency used in this invoice" }
                    }
                }
            };
        }
    }
}
