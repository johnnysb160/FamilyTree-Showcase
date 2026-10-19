using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FamilyTree.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSynthetic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Relatives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OtherNames = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Nickname = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Sex = table.Column<string>(type: "TEXT", maxLength: 1, nullable: false),
                    Relationship = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    BirthPlace = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    BirthCountry = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    IsDeceased = table.Column<bool>(type: "INTEGER", nullable: true),
                    DeathDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DeathPlace = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    BurialPlace = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    FatherId = table.Column<int>(type: "INTEGER", nullable: true),
                    MotherId = table.Column<int>(type: "INTEGER", nullable: true),
                    SpouseId = table.Column<int>(type: "INTEGER", nullable: true),
                    SpouseName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Occupation = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    TaxId = table.Column<string>(type: "TEXT", maxLength: 14, nullable: true),
                    PhotoUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Biography = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Sources = table.Column<string>(type: "TEXT", nullable: true),
                    IsRoot = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relatives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Relatives_Relatives_FatherId",
                        column: x => x.FatherId,
                        principalTable: "Relatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Relatives_Relatives_MotherId",
                        column: x => x.MotherId,
                        principalTable: "Relatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Relatives_Relatives_SpouseId",
                        column: x => x.SpouseId,
                        principalTable: "Relatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Relatives",
                columns: new[] { "Id", "Biography", "BirthCountry", "BirthDate", "BirthPlace", "BurialPlace", "Confidence", "DeathDate", "DeathPlace", "FatherId", "IsDeceased", "IsRoot", "MotherId", "Name", "Nickname", "Notes", "Occupation", "OtherNames", "PhotoUrl", "Relationship", "Sex", "Sources", "SpouseId", "SpouseName", "TaxId" },
                values: new object[,]
                {
                    { 4, null, "Brasil", new DateOnly(1992, 11, 5), "Porto Claro", null, "Confirmado", null, null, null, false, false, null, "Sofia Martins", null, null, null, null, null, "Esposa", "F", null, null, null, null },
                    { 11, null, "Brasil", new DateOnly(1910, 6, 24), "Serra Azul", null, "Confirmado", new DateOnly(1992, 8, 11), null, null, true, false, null, "Beatriz Rocha", null, null, null, null, null, "Bisavó paterna", "F", null, null, null, null },
                    { 13, null, "Portugal", new DateOnly(1913, 4, 1), "Vila das Fontes", null, "Confirmado", new DateOnly(1998, 12, 20), null, null, true, false, null, "Rosa Pereira", null, null, null, null, null, "Bisavó paterna", "F", null, null, null, null },
                    { 15, null, "Brasil", new DateOnly(1911, 1, 26), "Santa Aurora", null, "Confirmado", new DateOnly(2001, 9, 14), null, null, true, false, null, "Amelia Santos", null, null, null, null, null, "Bisavó materna", "F", null, null, null, null },
                    { 17, null, "Brasil", new DateOnly(1914, 8, 22), "Campo Dourado", null, "Confirmado", new DateOnly(2003, 5, 18), null, null, true, false, null, "Laura Ferreira", null, null, null, null, null, "Bisavó materna", "F", null, null, null, null },
                    { 19, null, "Brasil", new DateOnly(1987, 6, 28), "Vale Sereno", null, "Confirmado", null, null, null, false, false, null, "Miguel Sousa", null, null, null, null, null, "Cunhado", "M", null, null, null, null },
                    { 22, null, "Brasil", new DateOnly(1968, 3, 25), "Nova Esperança", null, "Confirmado", null, null, null, false, false, null, "Patricia Lima", null, null, null, null, null, "Tia por afinidade", "F", null, null, null, null },
                    { 10, null, "Brasil", new DateOnly(1905, 12, 2), "Serra Azul", null, "Confirmado", new DateOnly(1980, 4, 19), null, null, true, false, null, "Joaquim Almeida", null, null, null, null, null, "Bisavô paterno", "M", null, 11, null, null },
                    { 12, null, "Portugal", new DateOnly(1908, 10, 8), "Vila das Fontes", null, "Confirmado", new DateOnly(1985, 2, 6), null, null, true, false, null, "Manuel Duarte", null, null, null, null, null, "Bisavô paterno", "M", null, 13, null, null },
                    { 14, null, "Brasil", new DateOnly(1906, 7, 17), "Santa Aurora", null, "Confirmado", new DateOnly(1979, 3, 29), null, null, true, false, null, "Eduardo Costa", null, null, null, null, null, "Bisavô materno", "M", null, 15, null, null },
                    { 16, null, "Brasil", new DateOnly(1909, 11, 13), "Campo Dourado", null, "Confirmado", new DateOnly(1988, 7, 5), null, null, true, false, null, "Bento Nunes", null, null, null, null, null, "Bisavô materno", "M", null, 17, null, null },
                    { 7, null, "Brasil", new DateOnly(1938, 9, 30), "Nova Esperança", null, "Confirmado", null, null, 12, false, false, 13, "Helena Duarte", null, null, null, null, null, "Avó paterna", "F", null, null, null, null },
                    { 9, null, "Brasil", new DateOnly(1940, 3, 16), "Campo Dourado", null, "Confirmado", null, null, 16, false, false, 17, "Teresa Nunes", null, null, null, null, null, "Avó materna", "F", null, null, null, null },
                    { 25, null, "Portugal", new DateOnly(1936, 7, 19), "Vila das Fontes", null, "Confirmado", null, null, 12, false, false, 13, "Tomas Duarte", null, null, null, null, null, "Tio-avô paterno", "M", null, null, null, null },
                    { 6, null, "Brasil", new DateOnly(1933, 1, 14), "Serra Azul", null, "Confirmado", new DateOnly(2010, 6, 3), null, 10, true, false, 11, "Antonio Almeida", null, null, null, null, null, "Avô paterno", "M", null, 7, null, null },
                    { 8, null, "Brasil", new DateOnly(1935, 5, 9), "Santa Aurora", null, "Confirmado", new DateOnly(2016, 10, 27), null, 14, true, false, 15, "Paulo Costa", null, null, null, null, null, "Avô materno", "M", null, 9, null, null },
                    { 3, null, "Brasil", new DateOnly(1964, 2, 21), "Santa Aurora", null, "Confirmado", null, null, 8, false, false, 9, "Marina Costa", null, null, null, null, null, "Mãe", "F", null, null, null, null },
                    { 21, null, "Brasil", new DateOnly(1967, 10, 2), "Vale Sereno", null, "Confirmado", null, null, 6, false, false, 7, "Daniel Almeida", null, null, null, null, null, "Tio paterno", "M", null, 22, null, null },
                    { 24, null, "Brasil", new DateOnly(1969, 5, 14), "Santa Aurora", null, "Confirmado", null, null, 8, false, false, 9, "Rosa Costa", null, null, null, null, null, "Tia materna", "F", null, null, null, null },
                    { 2, null, "Brasil", new DateOnly(1962, 8, 7), "Vale Sereno", null, "Confirmado", null, null, 6, false, false, 7, "Rafael Almeida", null, null, null, null, null, "Pai", "M", null, 3, null, null },
                    { 23, null, "Brasil", new DateOnly(1994, 9, 11), "Vale Sereno", null, "Confirmado", null, null, 21, false, false, 22, "Gabriela Almeida", null, null, null, null, null, "Prima", "F", null, null, null, null },
                    { 1, null, "Brasil", new DateOnly(1990, 4, 18), "Porto Claro", null, "Confirmado", null, null, 2, false, true, 3, "Lucas Almeida", null, null, null, null, null, "Você", "M", null, 4, null, null },
                    { 18, null, "Brasil", new DateOnly(1988, 12, 9), "Porto Claro", null, "Confirmado", null, null, 2, false, false, 3, "Beatriz Almeida", null, null, null, null, null, "Irmã", "F", null, 19, null, null },
                    { 5, null, "Brasil", new DateOnly(2018, 7, 12), "Porto Claro", null, "Confirmado", null, null, 1, false, false, 4, "Clara Almeida", null, null, null, null, null, "Filha", "F", null, null, null, null },
                    { 20, null, "Brasil", new DateOnly(2015, 2, 3), "Porto Claro", null, "Confirmado", null, null, 19, false, false, 18, "Theo Sousa", null, null, null, null, null, "Sobrinho", "M", null, null, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Relatives_FatherId",
                table: "Relatives",
                column: "FatherId");

            migrationBuilder.CreateIndex(
                name: "IX_Relatives_MotherId",
                table: "Relatives",
                column: "MotherId");

            migrationBuilder.CreateIndex(
                name: "IX_Relatives_Name",
                table: "Relatives",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Relatives_SpouseId",
                table: "Relatives",
                column: "SpouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Relatives");
        }
    }
}
