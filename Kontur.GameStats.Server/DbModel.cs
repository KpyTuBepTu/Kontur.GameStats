using System;
using System.Collections.Generic;
using System.Data.Entity;
using SQLite.CodeFirst;
using System.Data.SQLite;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kontur.GameStats.Server
{
    public class DbModel : DbContext
    {
        public DbModel(string path)
			: base(new SQLiteConnection
			{
				ConnectionString = new SQLiteConnectionStringBuilder
				{
					DataSource = path,
					ForeignKeys = true,
					BinaryGUID = false,
				}.ConnectionString
			}, true)
		{ }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<DbModel>(modelBuilder);
            Database.SetInitializer(sqliteConnectionInitializer);
        }

        public virtual DbSet<Scoreboards> Scoreboards { get; set; }
        public virtual DbSet<Matches> Matches { get; set; }
        public virtual DbSet<Servers> Servers { get; set; }
        public virtual DbSet<GameModes> GameModes { get; set; }
        public virtual DbSet<Players> Players { get; set; }
        public virtual DbSet<Errors> Errors { get; set; }
    }

    public class Servers
    {
        public Servers()
        {
            Matches = new HashSet<Matches>();
            Modes = new HashSet<GameModes>();

            RecordTimeCreation = DateTime.Now;
        }

        [Key]
        [Column("Id", Order = 1)]
        public Int64 Id { get; set; }

        [Required]
        [Column(TypeName = "varchar")]
        [MaxLength(50)]
        public string Endpoint { get; set; }

        [Required]
        [Column(TypeName = "varchar")]
        [MaxLength(50)]
        public string Name { get; set; }

        [Timestamp]
        [Column(TypeName = "datetime")]
        private DateTime RecordTimeCreation { get; set; }

        public virtual ICollection<Matches> Matches { get; set; }

        public virtual ICollection<GameModes> Modes { get; set; }
    }

    public class Matches
    {
        public Matches()
        {
            Scoreboard = new HashSet<Scoreboards>();

            RecordTimeCreation = DateTime.Now;
        }

        [Key]
        [Column("Id", Order = 1)]
        public Int64 Id { get; set; }

        [Required]
        public Int64 ServerId { get; set; }

        [ForeignKey("ServerId")]
        public virtual Servers Server { get; set; }

        [Required]
        public Int64 GameModeId { get; set; }

        [ForeignKey("GameModeId")]
        public virtual GameModes GameMode { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime Timestamp { get; set; }

        [Required]
        [Column(TypeName = "varchar")]
        [MaxLength(50)]
        public string Map { get; set; }

        [Required]
        [Column(TypeName = "int")]
        public int FragLimit { get; set; }

        [Required]
        [Column(TypeName = "float")]
        public double TimeLimit { get; set; }

        [Required]
        [Column(TypeName = "float")]
        public double TimeElapsed { get; set; }

        [Timestamp]
        [Column(TypeName = "datetime")]
        private DateTime RecordTimeCreation { get; set; }

        public virtual ICollection<Scoreboards> Scoreboard { get; set; }
    }

    public class Scoreboards
    {
        public Scoreboards()
        {
            RecordTimeCreation = DateTime.Now;
        }

        [Key]
        public Int64 Id { get; set; }

        [Required]
        public Int64 MatchId { get; set; }

        [ForeignKey("MatchId")]
        public virtual Matches Match { get; set; }

        [Required]
        public Int64 PlayerId { get; set; }

        [ForeignKey("PlayerId")]
        public virtual Players Player { get; set; }     

        [Required]
        [Column(TypeName = "int")]
        public int Frags { get; set; }

        [Required]
        [Column(TypeName = "int")]
        public int Kills { get; set; }

        [Required]
        [Column(TypeName = "int")]
        public int Death { get; set; }

        [Required]
        [Column(TypeName = "float")]
        public double ScoreboardPercent { get; set; }

        [Timestamp]
        [Column(TypeName = "datetime")]
        private DateTime RecordTimeCreation { get; set; }
    }

    public class GameModes
    {
        public GameModes()
        {
            RecordTimeCreation = DateTime.Now;

            Matches = new HashSet<Matches>();
            ServersSet = new HashSet<Servers>();
        }

        [Key]
        public Int64 Id { get; set; }

        [Required]
        [Column(TypeName = "varchar")]
        [MaxLength(10)]
        public string Mode { get; set; }

        [Timestamp]
        [Column(TypeName = "datetime")]
        private DateTime RecordTimeCreation { get; set; }

        public virtual ICollection<Matches> Matches { get; set; }

        public virtual ICollection<Servers> ServersSet { get; set; }
    }

    public class Players
    {
        public Players()
        {
            Scores = new HashSet<Scoreboards>();

            RecordTimeCreation = DateTime.Now;
        }

        [Key]
        public Int64 Id { get; set; }

        [Required]
        [Column(TypeName = "varchar")]
        [MaxLength(50)]
        public string Name { get; set; }

        [Timestamp]
        [Column(TypeName = "datetime")]
        private DateTime RecordTimeCreation { get; set; }

        public virtual ICollection<Scoreboards> Scores { get; set; }
    }

    public class Errors
    {
        public Errors()
        {
            RecordTimeCreation = DateTime.Now;
        }

        [Key]
        public Int64 Id { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime Timestamp { get; set; }

        [Required]
        [Column(TypeName = "varchar")]
        public string ErrorMessage { get; set; }

        [Required]
        [Column(TypeName = "varchar")]
        public string Url { get; set; }

        [Required]
        [Column(TypeName = "varchar")]
        [MaxLength(3)]
        public string Method { get; set; }

        [Timestamp]
        [Column(TypeName = "datetime")]
        private DateTime RecordTimeCreation { get; set; }
    }
}