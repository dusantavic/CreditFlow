using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Applicants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    NationalId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EmployerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MonthlyIncomeAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MonthlyIncomeCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    YearsEmployed = table.Column<int>(type: "integer", nullable: false),
                    ExistingMonthlyDebtAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExistingMonthlyDebtCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DebtLastCheckedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RegisteredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applicants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoanApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RequestedAmountCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    RequestedTermMonths = table.Column<int>(type: "integer", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreditScore = table.Column<int>(type: "integer", nullable: true),
                    RiskTier = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DebtToIncomeRatioPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    AssessmentReasons = table.Column<string>(type: "text", nullable: true),
                    AssessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedPrincipalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ApprovedPrincipalCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    AnnualInterestRatePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ApprovedTermMonths = table.Column<int>(type: "integer", nullable: true),
                    MonthlyPaymentAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MonthlyPaymentCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    RejectionReasons = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanApplications", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Applicants");

            migrationBuilder.DropTable(
                name: "LoanApplications");
        }
    }
}
