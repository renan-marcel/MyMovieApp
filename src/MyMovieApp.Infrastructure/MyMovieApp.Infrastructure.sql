CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Movies" (
    "ImdbId" text NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Year" integer NOT NULL,
    "Genre" text NOT NULL,
    "Director" text NOT NULL,
    "ImdbRating" text NOT NULL,
    "Plot" text NOT NULL,
    CONSTRAINT "PK_Movies" PRIMARY KEY ("ImdbId")
);

CREATE TABLE "Actors" (
    "Id" uuid NOT NULL,
    "Name" character varying(1000) NOT NULL,
    "MovieImdbId" text NOT NULL,
    CONSTRAINT "PK_Actors" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Actors_Movies_MovieImdbId" FOREIGN KEY ("MovieImdbId") REFERENCES "Movies" ("ImdbId") ON DELETE CASCADE
);

CREATE TABLE "Reviews" (
    "Id" uuid NOT NULL,
    "UserOpinion" character varying(1000) NOT NULL,
    "UserRating" integer NOT NULL,
    "ImdbId" text NOT NULL,
    CONSTRAINT "PK_Reviews" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Reviews_Movies_ImdbId" FOREIGN KEY ("ImdbId") REFERENCES "Movies" ("ImdbId") ON DELETE CASCADE
);

CREATE INDEX "IX_Actors_MovieImdbId" ON "Actors" ("MovieImdbId");

CREATE INDEX idx_movies_title_year ON "Movies" ("Title", "Year");

CREATE INDEX "IX_Reviews_ImdbId" ON "Reviews" ("ImdbId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250618043336_CreateInitial', '8.0.17');

COMMIT;

