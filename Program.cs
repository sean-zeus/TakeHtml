
using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Dapper;
using Microsoft.EntityFrameworkCore;
using TakeHtml.Models.AvDB;
using Microsoft.Data.SqlClient;
using System.Data;

//using System.Data.SQLite;

var builder = WebApplication.CreateBuilder(args);

//CORS(下面的Configure也需啟用app.UseCors())
var corsOrigins = builder.Configuration.GetSection("CORS:AllowOrigin").Get<string[]>();
builder.Services.AddCors(opt => opt.AddDefaultPolicy(builder =>
{
    if (corsOrigins.Contains("*"))
    {
        builder.SetIsOriginAllowed(_ => true);
        //不透過appsettings.json直接設定localhost
        //builder.SetIsOriginAllowed(_=>new Uri(_).Host == "localhost")
    }
    else
    {
        builder.WithOrigins(corsOrigins);
        //不透過appsettings.json直接設定
        //.WithOrigins(new string[] {"www.exam1.com","www.exam2.com" })
    }
    builder.AllowAnyMethod();
    builder.AllowAnyHeader();
    builder.AllowCredentials();
    //AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); //只能在AllowAnyOrigin和AllowCredentials()二選一、SetIsOriginAllowed出現後可以
}));

// 設置 JSON 序列化選項
builder.Services.AddControllers().AddJsonOptions(options =>
{
    //Set the JSON serializer options
    options.JsonSerializerOptions.PropertyNamingPolicy = null; //屬性的名稱轉換為另一個格式的原則，預設為小寫開頭的駝峰式命名（camelCase）駝峰式大寫（cPascalCase），或 null 以讓屬性名稱保持不變。
    //options.JsonSerializerOptions.PropertyNameCaseInsensitive = false; //屬性名稱不區分大小寫
    //options.JsonSerializerOptions.AllowTrailingCommas = true;  //尾隨逗號
    options.JsonSerializerOptions.WriteIndented = true;  //縮排空白
    //options.JsonSerializerOptions.IgnoreNullValues = true; //不轉換內容為null的欄位
    options.JsonSerializerOptions.Converters.Add(new NullableDateTimeConverter("yyyy-MM-dd HH:mm:ss")); //日期時間格式轉換
});


//if (!File.Exists($".\\{DateTime.Now.Year}EventLog.db"))
//{
//    HelperDBEventLog.InitSQLiteDb();
//}

// 設置 EF Context連線資訊
// SqlServer
//builder.Services.AddDbContext<MyDBContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("MyDB")));
//builder.Services.AddDbContext<WebAPIContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("WebAPI")));
//builder.Services.AddDbContext<devrepContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("devrep")));
// Sqlite
builder.Services.AddDbContext<AvDBContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("AvDBContext"))); //Scaffold-DbContext "Data Source=./Dbase/AvDB.db" Microsoft.EntityFrameworkCore.Sqlite -OutputDir Models/AvDB -Force -CoNtext AvDBContext
// MySql、MariaDB
// builder.Services.AddDbContext<socContext>(options => options.UseMySql(builder.Configuration.GetConnectionString("MariaDB_Soc"),mysqlOptions =>{ mysqlOptions.ServerVersion(new Version(10, 2, 10), ServerType.MariaDb); }));

//手動寫的依賴注入
//builder.Services.AddScoped<WebApiRepositoryDI>();

//方法一 Dapper的依賴注入
builder.Services.AddTransient<IDbConnection>(db => new SqliteConnection(builder.Configuration.GetConnectionString("AvDBContext")));
//private readonly IDbConnection _Ocn;
//public HomeController(IDbConnection Ocn) => _Ocn = Ocn;

//方法二 Dapper的依賴注入 進階(簡單工廠 + Singleton來避免資源浪費)需搭配下方介面引入作法
//builder.Services.AddSingleton<IConnectionFactory>(_ => new SqlConnectionFactory(() => builder.Configuration.GetConnectionString("AvDBContext")));
//後續在control裏引入作法
//public class HomeController : Controller
//{
//    private readonly IConnectionFactory _factory;
//    public HomeController(IConnectionFactory factory) => this._factory = factory;

//    public string Index()
//    {
//        //..驗證資料,假如資料格式錯誤,不需要建立Connection
//        using (var cnn = this._factory.CreateConnection())
//        {
//            var result = cnn.QueryFirst<string>("select 'Hello World' message");
//            return result;
//        }
//    }
//}


var app = builder.Build();

app.UseCors();//CORS(上面的ConfigureServices也需設定services.AddCors)

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();

app.Run();

#region 轉換日期格式的寫法
public class NullableDateTimeConverter : JsonConverter<DateTime?>
{
    private string Format = "yyyy-MM-dd HH:mm";
    public NullableDateTimeConverter(string Format) => this.Format = Format;

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString() is { } value && DateTime.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) ? result : null;
    }
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is { })
        {
            writer.WriteStringValue(value.Value.ToString(Format, CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
#endregion

#region 進階(簡單工廠 + Singleton來避免資源浪費)需搭配方作法
public interface IConnectionFactory
{
    IDbConnection CreateConnection();
}

public class SqlConnectionFactory : IConnectionFactory
{
    private readonly Func<string> _getConnectionString;
    public SqlConnectionFactory(Func<string> getConnectionString) => this._getConnectionString = getConnectionString;
    public IDbConnection CreateConnection() => new SqlConnection(_getConnectionString());
}
#endregion

#region HelperDBEventLog
public class HelperDBEventLog
{
    public class Log
    {
        public long Id { get; set; }
        public DateTime CrTime { get; set; }
        public string Path { get; set; }
        public string Method { get; set; }
        public string User { get; set; }
        public string Account { get; set; }
        public string SourceIp { get; set; }
        public string Message { get; set; }
        public string Other { get; set; }
    }

    static string dbPath = $".\\{DateTime.Now.Year}EventLog.db";
    static string OcnStr = "Data Source=" + dbPath;

    #region 新增SysLog紀錄
    /// <summary>新增SysLog紀錄<br/>
    /// 參數1:context(HttpContext)、參數2:_message(string)、參數3:_other(string)<br/>
    /// 返回: Task＜String＞
    /// <code>
    /// 使用方法
    /// await HelperDBEventLog.AddSysLog(HttpContext, $"{GetType().Name} => 登入成功:使用者:{user.人員姓名}＜{user.帳號}＞");
    /// </code>
    /// </summary>
    public static async Task<String> AddSysLog(HttpContext context, string _message = "", string _other = "")
    {
        using (var Ocn = new SqliteConnection(OcnStr))
        {
            try
            {
                //var _fullname = ""; var _account = "";
                //if (!String.IsNullOrWhiteSpace(context.Request.Headers["Authorization"].ToString()) && context.Request.Headers["Authorization"].ToString() != "Bearer token")
                //{
                //    var _authToken = context.Request.Headers["Authorization"].ToString().EM_AesDecrypt().Split('卍');

                //    _fullname = _authToken[1] ?? "";
                //    _account = _authToken[2] ?? "";
                //}

                //var _path = context.Request.EM_GetAbsoluteUri() ?? "";
                //var _method = context.Request.Method ?? "";
                //var _clintip = context.EM_GetUserIp() ?? "";
                //var _data = new { Path = _path, Method = _method, User = _fullname, Account = _account, SourceIp = _clintip, Message = _message, Other = _other };

                //await Ocn.ExecuteAsync("INSERT INTO SysLog(Path,Method,User,Account,SourceIp,Message,Other) VALUES(@Path, @Method, @User, @Account, @SourceIp, @Message, @Other)", _data);
                return "OK";
            }
            catch (Exception ex)
            {
                return $"Err:{ex.Message}";
            }
        }
    }
    #endregion

    #region 新增ErrLog紀錄
    /// <summary>新增ErrLog紀錄<br/>
    /// 參數1:context(HttpContext)、參數2:_message(string)、參數3:_other(string)<br/>
    /// 返回: Task＜String＞
    /// <code>
    /// 使用方法
    /// await HelperDBEventLog.AddErrLog(HttpContext, $"{GetType().Name} => 登入成功:使用者:{user.人員姓名}＜{user.帳號}＞");
    /// </code>
    /// </summary>
    public static async Task<String> AddErrLog(HttpContext context, string _message = "", string _other = "")
    {
        using (var Ocn = new SqliteConnection(OcnStr))
        {
            try
            {
                //if (context.Request.Cookies[".AspNetCore.Session"] != null)
                //{
                //    var _cookies = context.Request.Cookies[".AspNetCore.Session"].EM_AesDecrypt().Split('卍');

                //    var _path = context.Request.EM_GetAbsoluteUri() ?? "";
                //    var _method = context.Request.Method ?? ""; ;
                //    var _fullname = _cookies[1] ?? "";
                //    var _account = _cookies[2] ?? "";
                //    var _clintip = context.EM_GetUserIp() ?? "";

                //    var _data = new { Path = _path, Method = _method, User = _fullname, Account = _account, SourceIp = _clintip, Message = _message, Other = _other };

                //    await Ocn.ExecuteAsync("INSERT INTO ErrLog(Path,Method,User,Account,SourceIp,Message,Other) VALUES(@Path, @Method, @User, @Account, @SourceIp, @Message, @Other)", _data);
                //}
                return "OK";
            }
            catch (Exception ex)
            {
                return $"Err:{ex.Message}";
            }
        }
    }
    #endregion

    public static void InitSQLiteDb()
    {
        if (File.Exists(dbPath)) return;
        using (var Ocn = new SqliteConnection(OcnStr))
        {
            Ocn.Execute(
                @"CREATE TABLE ErrLog (
                        Id INTEGER  PRIMARY KEY AUTOINCREMENT UNIQUE NOT NULL,
                        CrTime DATETIME DEFAULT (datetime('now', 'localtime')) NOT NULL,
                        Path TEXT DEFAULT """" NOT NULL, 
                        Method TEXT DEFAULT """"  NOT NULL,
                        User TEXT DEFAULT """" NOT NULL,
                        Account TEXT DEFAULT """" NOT NULL,
                        SourceIP TEXT DEFAULT """" NOT NULL,
                        Message TEXT DEFAULT """" NOT NULL,
                        Other TEXT DEFAULT """" NOT NULL
                    )"
            );
            Ocn.Execute(
                @"CREATE TABLE SysLog (
                        Id INTEGER  PRIMARY KEY AUTOINCREMENT UNIQUE NOT NULL,
                        CrTime DATETIME DEFAULT (datetime('now', 'localtime')) NOT NULL,
                        Path TEXT DEFAULT """" NOT NULL, 
                        Method TEXT DEFAULT """"  NOT NULL,
                        User TEXT DEFAULT """" NOT NULL,
                        Account TEXT DEFAULT """" NOT NULL,
                        SourceIP TEXT DEFAULT """" NOT NULL,
                        Message TEXT DEFAULT """" NOT NULL,
                        Other TEXT DEFAULT """" NOT NULL
                    )"
            );

            var mess1 = TestInsert();
            //var mess2 = TestSelect();
            //var mess3 = TestErr();
        }
    }

    static string TestInsert()
    {
        using (var Ocn = new SqliteConnection(OcnStr))
        {
            try
            {
                //先刪除資料
                //Ocn.Execute("DELETE FROM SysLog");

                //测试数据新增多筆參數
                var TestData = new[] {
                        new { Id= 0, Crtime= DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Path= "XXX", Method= "XXX", User= "系統管理者", Account= "System", SourceIp= "127.0.0.1", Message= "資料庫建立時初始資料", Other= "" },
                        new { Id= 1, Crtime= DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Path= "XXX", Method= "XXX", User= "B", Account= "B", SourceIp= "B", Message= "B", Other= "B"   },
                        new { Id= 2, Crtime= DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Path= "XXX", Method= "XXX", User= "C", Account= "C", SourceIp= "C", Message= "C", Other= "C"   }
                    };

                Ocn.Execute("INSERT INTO SysLog VALUES(@Id, @Crtime, @Path, @Method, @User, @Account, @SourceIp, @Message, @Other)", TestData[0]);
                Ocn.Execute("INSERT INTO ErrLog VALUES(@Id, @Crtime, @Path, @Method, @User, @Account, @SourceIp, @Message, @Other)", TestData[0]);

                return "OK";
            }
            catch (Exception ex)
            {
                return $"Err:{ex.Message}";
            }
        }
    }

    static string TestSelect()
    {
        using (var Ocn = new SqliteConnection(OcnStr))
        {
            //重整資料庫
            Ocn.Execute("VACUUM");

            //讀取資料庫結構
            var list = Ocn.Query("PRAGMA table_info(SysLog)");

            //var jsonstr = JsonConvert.SerializeObject(list, Formatting.Indented); //json.net
            var jsonstr = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });

            return jsonstr;
        }
    }

    //---遍历查询表结构
    static void QueryAllTableInfo()
    {
        //如果不止一个表，要遍历所有表的结构如下，就要用到 SQLite 中的特殊表 sqlite_master，它的结构如下：
        //参考：
        //2.6.Storage Of The SQL Database Schema
        //CREATE TABLE sqlite_master(
        //  type text,
        //  name text,
        //  tbl_name text,
        //  rootpage integer,
        //  sql text
        //);
        //当 type = table 时，name 和 tbl_name 是一样的，其他比如 type = index 、view 之类时，tbl_name 才是表名。

        string path = @"d:\test\123.sqlite";
        SqliteConnection cn = new SqliteConnection("data source=" + path);
        if (cn.State != System.Data.ConnectionState.Open)
        {
            cn.Open();
            SqliteCommand cmd = new SqliteCommand();
            cmd.Connection = cn;
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE TYPE='table' ";
            SqliteDataReader sr = cmd.ExecuteReader();
            List<string> tables = new List<string>();
            while (sr.Read())
            {
                tables.Add(sr.GetString(0));
            }
            //datareader 必须要先关闭，否则 commandText 不能赋值
            sr.Close();
            foreach (var a in tables)
            {
                cmd.CommandText = $"PRAGMA TABLE_INFO({a})";
                sr = cmd.ExecuteReader();
                while (sr.Read())
                {
                    Console.WriteLine($"{sr[0]} {sr[1]} {sr[2]} {sr[3]}");
                }
                sr.Close();
            }
        }
        cn.Close();
    }

    static string TestErr()
    {
        using (var Ocn = new SqliteConnection(OcnStr))
        {
            //測試Primary Key
            try
            {
                //故意塞入錯誤資料
                var insertScript = "INSERT INTO SysLog VALUES(@Id, @Crtime, @Path, @Method, @User, @Account, @SourceIp, @Message, @Other)";
                Ocn.Execute(insertScript, new { Id = 0, Crtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Path = "XXX", Method = "XXX", User = "系統管理者", Account = "System", SourceIp = "127.0.0.1", Message = "資料庫建立時初始資料", Other = "" });

                return "失敗：未阻止資料重複";
            }
            catch (Exception ex)
            {
                return $"測試成功:{ex.Message}";
            }
        }
    }
}
#endregion
