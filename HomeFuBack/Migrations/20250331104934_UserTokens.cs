using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeFuBack.Migrations
{
	/// <inheritdoc />
	public partial class UserTokens : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// Удаляем первичный ключ из таблицы Users
			migrationBuilder.DropPrimaryKey(
				name: "PK_Users",
				table: "Users");

			// Удаляем столбец Id
			migrationBuilder.Sql(@"
                ALTER TABLE [Users] DROP COLUMN [Id];
            ");

			// Добавляем столбец Id с типом Guid и разрешаем NULL
			migrationBuilder.AddColumn<Guid>(
				name: "Id",
				table: "Users",
				type: "uniqueidentifier",
				nullable: true);

			// Обновляем таблицу Users, присваивая новые Guid значения
			migrationBuilder.Sql(@"
                UPDATE [Users] SET [Id] = NEWID();
            ");

			// Устанавливаем столбец Id как NOT NULL
			migrationBuilder.Sql(@"
                ALTER TABLE [Users] ALTER COLUMN [Id] uniqueidentifier NOT NULL;
            ");

			// Добавляем новый первичный ключ
			migrationBuilder.AddPrimaryKey(
				name: "PK_Users",
				table: "Users",
				column: "Id");

			migrationBuilder.AddColumn<string>(
				name: "EmailConfirmCode",
				table: "Users",
				type: "nvarchar(max)",
				nullable: true);

			migrationBuilder.AddColumn<string>(
				name: "Role",
				table: "Users",
				type: "nvarchar(max)",
				nullable: true);

			// Создаем таблицу Tokens
			migrationBuilder.CreateTable(
				name: "Tokens",
				columns: table => new
				{
					id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
					UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
					SubmitDt = table.Column<DateTime>(type: "datetime2", nullable: false),
					ExpireDt = table.Column<DateTime>(type: "datetime2", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Tokens", x => x.id);
					table.ForeignKey(
						name: "FK_Tokens_Users_UserId",
						column: x => x.UserId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			// Создаем индекс в таблице Tokens
			migrationBuilder.CreateIndex(
				name: "IX_Tokens_UserId",
				table: "Tokens",
				column: "UserId");

			// Удаляем внешний ключ из таблицы Tokens, если он есть
			if (migrationBuilder.IsSqlServer())
			{
				migrationBuilder.DropForeignKey(
					name: "FK_Tokens_Users_UserId",
					table: "Tokens");
			}
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "Tokens");

			migrationBuilder.DropColumn(
				name: "EmailConfirmCode",
				table: "Users");

			migrationBuilder.DropColumn(
				name: "Role",
				table: "Users");

			// Удаляем первичный ключ
			migrationBuilder.DropPrimaryKey(
				name: "PK_Users",
				table: "Users");

			// Удаляем столбец Id
			migrationBuilder.Sql(@"
                ALTER TABLE [Users] DROP COLUMN [Id];
            ");

			// Добавляем столбец Id с типом int и identity
			migrationBuilder.AddColumn<int>(
				name: "Id",
				table: "Users",
				type: "int",
				nullable: false)
				.Annotation("SqlServer:Identity", "1, 1");

			// Добавляем первичный ключ обратно
			migrationBuilder.AddPrimaryKey(
				name: "PK_Users",
				table: "Users",
				column: "Id");

			// Добавляем внешний ключ обратно в таблицу Tokens
			if (migrationBuilder.IsSqlServer())
			{
				migrationBuilder.AddForeignKey(
					name: "FK_Tokens_Users_UserId",
					table: "Tokens",
					column: "UserId",
					principalTable: "Users",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			}
		}
	}
}