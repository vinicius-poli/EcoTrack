using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarLicencas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TBLicenca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpreendimentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroProcesso = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NumeroLicenca = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrgaoEmissor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    DataEmissao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataValidade = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiasAntecedenciaRenovacao = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    NumeroProtocoloRenovacao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DataProtocoloRenovacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBLicenca", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBLicenca_TBEmpreendimento_EmpreendimentoId",
                        column: x => x.EmpreendimentoId,
                        principalTable: "TBEmpreendimento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TBLicenca_EmpreendimentoId",
                table: "TBLicenca",
                column: "EmpreendimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_TBLicenca_NumeroLicenca",
                table: "TBLicenca",
                column: "NumeroLicenca");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TBLicenca");
        }
    }
}
