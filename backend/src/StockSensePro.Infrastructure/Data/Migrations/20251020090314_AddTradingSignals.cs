using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSensePro.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTradingSignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TradingSignals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Strategy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SignalType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConfidenceScore = table.Column<int>(type: "integer", nullable: false),
                    EntryPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    TargetPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    StopLoss = table.Column<decimal>(type: "numeric", nullable: true),
                    TakeProfit = table.Column<decimal>(type: "numeric", nullable: true),
                    Rationale = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActualReturn = table.Column<decimal>(type: "numeric", nullable: true),
                    HoldingPeriodDays = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingSignals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignalPerformances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TradingSignalId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualReturn = table.Column<decimal>(type: "numeric", nullable: false),
                    BenchmarkReturn = table.Column<decimal>(type: "numeric", nullable: false),
                    WasProfitable = table.Column<bool>(type: "boolean", nullable: false),
                    DaysHeld = table.Column<int>(type: "integer", nullable: false),
                    EntryPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    ExitPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxDrawdown = table.Column<decimal>(type: "numeric", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalPerformances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignalPerformances_TradingSignals_TradingSignalId",
                        column: x => x.TradingSignalId,
                        principalTable: "TradingSignals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SignalPerformances_EvaluatedAt",
                table: "SignalPerformances",
                column: "EvaluatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SignalPerformances_TradingSignalId",
                table: "SignalPerformances",
                column: "TradingSignalId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingSignals_Symbol",
                table: "TradingSignals",
                column: "Symbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignalPerformances");

            migrationBuilder.DropTable(
                name: "TradingSignals");
        }
    }
}
