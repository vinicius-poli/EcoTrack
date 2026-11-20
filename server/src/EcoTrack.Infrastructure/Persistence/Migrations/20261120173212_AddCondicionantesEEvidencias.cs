using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCondicionantesEEvidencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TBCondicionante",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LicencaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoItem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Periodicidade = table.Column<int>(type: "int", nullable: false),
                    DataVencimento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResponsavelTecnico = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBCondicionante", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBCondicionante_TBLicenca_LicencaId",
                        column: x => x.LicencaId,
                        principalTable: "TBLicenca",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TBEvidenciaCondicionante",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CondicionanteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnviadoPorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeOriginalArquivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    IdentificadorArmazenamento = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    NumeroArt = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DataUploadUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBEvidenciaCondicionante", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBEvidenciaCondicionante_TBCondicionante_CondicionanteId",
                        column: x => x.CondicionanteId,
                        principalTable: "TBCondicionante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TBCondicionante_LicencaId",
                table: "TBCondicionante",
                column: "LicencaId");

            migrationBuilder.CreateIndex(
                name: "IX_TBCondicionante_LicencaId_CodigoItem",
                table: "TBCondicionante",
                columns: new[] { "LicencaId", "CodigoItem" });

            migrationBuilder.CreateIndex(
                name: "IX_TBCondicionante_TenantId",
                table: "TBCondicionante",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TBEvidenciaCondicionante_CondicionanteId",
                table: "TBEvidenciaCondicionante",
                column: "CondicionanteId");

            migrationBuilder.CreateIndex(
                name: "IX_TBEvidenciaCondicionante_TenantId",
                table: "TBEvidenciaCondicionante",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TBEvidenciaCondicionante");

            migrationBuilder.DropTable(
                name: "TBCondicionante");
        }
    }
}
