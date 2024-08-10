using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Types;
using KamioniApi;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.IO;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

using System.IO;



var builder = WebApplication.CreateBuilder(args);


// Dodajte servise za CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});



// Dodajte servise za Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "KamioniApi", Version = "v1" });
});

var configuration = builder.Configuration; // Dodajte ovo

var app = builder.Build();



// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "KamioniApi v1");
    c.RoutePrefix = string.Empty; // To access Swagger directly from the root URL
});

// Dodajte middleware za CORS
app.UseCors("AllowAllOrigins");

app.UseHttpsRedirection();

/*if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}*/

var connectionString = configuration.GetConnectionString("DefaultConnection");
var library = new Library(connectionString);

app.MapPost("/api/testconnection", () =>
{
    try
    {
        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            return Results.Ok("Connection successful");
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
}).WithName("TestConnection");


//Poslodavac----------------------------------------------------------------------------------

app.MapPost("/api/Poslodavac/Dodaj", (string username, string password, string email, string naziv, string telefon) =>
{
    if (library.ProveraUsernamePoslodavac(username))
    {
        return Results.BadRequest("Username already exists.");
    }

    library.DodajPoslodavca(username, password, email, naziv, telefon);
    return Results.Ok("Success");
})
.WithName("DodajPoslodavca");

app.MapPut("/api/Poslodavac/{id}", (int id, string? username, string? password, string? email, string? naziv, string? telefon) =>
{
    try
    {
        if (!string.IsNullOrEmpty(username))
        {
            library.UpdatePoslodavacUsername(id, username);
        }
        if (!string.IsNullOrEmpty(password))
        {
            library.UpdatePoslodavacPassword(id, password);
        }
        if (!string.IsNullOrEmpty(email))
        {
            library.UpdatePoslodavacEmail(id, email);
        }
        if (!string.IsNullOrEmpty(naziv))
        {
            library.UpdatePoslodavacNaziv(id, naziv);
        }
        if (!string.IsNullOrEmpty(telefon))
        {
            library.UpdatePoslodavacTelefon(id, telefon);
        }

        return Results.Ok("Poslodavac successfully updated.");
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, title: "Username already exist", statusCode: 500);
    }
}).WithName("UpdatePoslodavac");

app.MapPost("/api/Poslodavac/ProveraLogin", (string username, string password) =>
{
    int userId = library.ProveraLoginPoslodavac(username, password);
    if (userId > 0)
    {
        DataTable poslodavac = library.VratiPoslodavcaPoId(userId);
        if (poslodavac.Rows.Count > 0)
        {
            string jsonResult = library.DataTableToJson(poslodavac);
            return Results.Ok(jsonResult);
        }
        else
        {
            return Results.NotFound("Poslodavac nije pronađen.");
        }
    }
    else
    {
        return Results.BadRequest("Invalid username or password.");
    }
}).WithName("ProveraLoginPoslodavac");

app.MapGet("/api/Poslodavac/{id}", (int id) =>
{
    // Dohvatanje podataka o poslodavcu
    DataTable poslodavac = library.VratiPoslodavcaPoId(id);

    if (poslodavac.Rows.Count > 0)
    {
        DataRow poslodavacRow = poslodavac.Rows[0];

        // Dohvatanje ukupnog broja ocena za poslodavca
        int totalOcena = library.UkupanBrojOcenaPoPoslodavacId(id);

        // Kreiranje rezultata sa dodatnim poljem za broj ocena
        var poslodavacObj = new
        {
            id = poslodavacRow["id"],
            username = poslodavacRow["username"],
            email = poslodavacRow["email"],
            naziv = poslodavacRow["naziv"],
            telefon = poslodavacRow["telefon"],
            ocena = poslodavacRow["ocena"],
            ukupanBrojOcena = totalOcena
        };

        return Results.Ok(poslodavacObj);
    }
    else
    {
        return Results.NotFound("Poslodavac nije pronađen.");
    }
})
.WithName("VratiPoslodavcaPoId");

app.MapPost("/api/OcenaPoslodavca/Dodaj", (int poslodavacId, int vozacId, int ocena, string komentar) =>
{
    try
    {
        library.DodajOcenuPoslodavca(poslodavacId, vozacId, ocena, komentar);
        return Results.Ok("Ocena successfully added.");
    }
    catch (Exception ex)
    {
        // Log the exception details (this is an example, you should log it properly in production)
        Console.WriteLine(ex.ToString());

        // Return a detailed error response
        return Results.Problem(detail: ex.Message, title: "An error occurred while adding the rating", statusCode: 500);
    }
}).WithName("DodajOcenuPoslodavca");


app.MapDelete("/api/OcenaPoslodavca", (int id) =>
{
    library.ObrisiOcenuPoslodavca(id);
    return Results.Ok("Ocena successfully deleted.");
})
.WithName("ObrisiOcenuPoslodavca");


app.MapGet("/api/OcenaPoslodavca/Poslodavac/{poslodavacId}", async (int poslodavacId) =>
{
    DataTable oceneDt = library.PrikaziOcenePoslodavcaPoPoslodavacId(poslodavacId);

    if (oceneDt.Rows.Count > 0)
    {
        var resultList = new List<object>();

        foreach (DataRow row in oceneDt.Rows)
        {
            int vozacId = Convert.ToInt32(row["vozac_id"]);
            DataTable vozacDt = library.VratiVozacaPoId(vozacId);
            DataTable poslodavacDt = library.VratiPoslodavcaPoId(poslodavacId);

            var ocenaObj = new
            {
                ocena = row["ocena"],
                komentar = row["komentar"],
                poslodavac = poslodavacDt.Rows.Count > 0 ? library.DataRowToDictionary(poslodavacDt.Rows[0]) : null,
                vozac = vozacDt.Rows.Count > 0 ? library.DataRowToDictionary(vozacDt.Rows[0]) : null
            };

            resultList.Add(ocenaObj);
        }

        return Results.Ok(resultList);
    }
    else
    {
        return Results.NotFound("Poslodavac ili ocene nisu pronađene.");
    }
})
.WithName("PrikaziOcenePoslodavcaPoPoslodavacId");

app.MapGet("/api/OcenaPoslodavca/Vozac/{vozacId}", async (int vozacId) =>
{
    DataTable oceneDt = library.PrikaziOcenePoslodavcaPoVozacId(vozacId);

    if (oceneDt.Rows.Count > 0)
    {
        var resultList = new List<object>();

        foreach (DataRow row in oceneDt.Rows)
        {
            int poslodavacId = Convert.ToInt32(row["poslodavac_id"]);
            DataTable vozacDt = library.VratiVozacaPoId(vozacId);
            DataTable poslodavacDt = library.VratiPoslodavcaPoId(poslodavacId);

            var ocenaObj = new
            {
                ocena = row["ocena"],
                komentar = row["komentar"],
                poslodavac = poslodavacDt.Rows.Count > 0 ? library.DataRowToDictionary(poslodavacDt.Rows[0]) : null,
                vozac = vozacDt.Rows.Count > 0 ? library.DataRowToDictionary(vozacDt.Rows[0]) : null
            };

            resultList.Add(ocenaObj);
        }

        return Results.Ok(resultList);
    }
    else
    {
        return Results.NotFound("Vozač ili ocene nisu pronađene.");
    }
})
.WithName("PrikaziOcenePoslodavcaPoVozacId");

app.MapGet("/api/Zahtevi/Poslodavac/{poslodavacId}", (int poslodavacId) =>
{
    try
    {
        var zahtevi = library.PrikaziZahtevePoPoslodavacId(poslodavacId);

        if (zahtevi.Count > 0)
        {
            return Results.Ok(zahtevi);
        }
        else
        {
            return Results.NotFound("Nema zahteva za datog poslodavca.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching requests: {ex.ToString()}");
        return Results.Problem(detail: ex.Message, title: "An error occurred while fetching requests", statusCode: 500);
    }
})
.WithName("PrikaziZahtevePoPoslodavacId");


app.MapPost("/api/Zahtevi/Dodaj", (int vozacId, int poslodavacId, int ponudaId) =>
{
    try
    {
        library.DodajZahtevZaPonudu(vozacId, poslodavacId, ponudaId);
        return Results.Ok("Zahtev je uspešno dodat.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error adding request: {ex.ToString()}");
        return Results.Problem(detail: ex.Message, title: "An error occurred while adding the request", statusCode: 500);
    }
})
.WithName("DodajZahtevZaPonudu");

app.MapDelete("/api/Zahtevi/{id}", (int id) =>
{
    try
    {
        library.ObrisiZahtevZaPonudu(id);
        return Results.Ok("Zahtev je uspešno obrisan.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error deleting request: {ex.ToString()}");
        return Results.Problem(detail: ex.Message, title: "An error occurred while deleting the request", statusCode: 500);
    }
})
.WithName("ObrisiZahtevZaPonudu");


//Vozac---------------------------------------------------------------------------------------


app.MapPost("/api/Vozac/Dodaj", (string username, string password, string email, string ime) =>
{
    if (library.ProveraUsernameVozac(username))
    {
        return Results.BadRequest("Username already exists.");
    }

    library.DodajVozaca(username, password, email, ime);
    return Results.Ok("Success");
})
.WithName("DodajVozaca");



app.MapPut("/api/Vozac/{id}", (int id, string? username, string? password, string? email, string? ime, string? vozilo, string? registracija, string? slikaBase64) =>
{
    try
    {
        if (!string.IsNullOrEmpty(username))
        {
            library.UpdateVozacUsername(id, username);
        }
        if (!string.IsNullOrEmpty(password))
        {
            library.UpdateVozacPassword(id, password);
        }
        if (!string.IsNullOrEmpty(email))
        {
            library.UpdateVozacEmail(id, email);
        }
        if (!string.IsNullOrEmpty(ime))
        {
            library.UpdateVozacIme(id, ime);
        }
        if (!string.IsNullOrEmpty(vozilo))
        {
            library.UpdateVozacVozilo(id, vozilo);
        }
        if (!string.IsNullOrEmpty(registracija))
        {
            library.UpdateVozacRegistracija(id, registracija);
        }
        if (!string.IsNullOrEmpty(slikaBase64))
        {
            byte[] slika = Convert.FromBase64String(slikaBase64);
            library.UpdateVozacSlika(id, slika);
        }

        return Results.Ok("Vozac successfully updated.");
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, title: "Username already exist", statusCode: 500);
    }
}).WithName("UpdateVozac");


app.MapPut("/api/Vozac/UpdateLokacija", (int id, double latitude, double longitude) =>
{
    try
    {
        library.UpdateVozacLokacija(id, latitude, longitude);
        return Results.Ok("Lokacija successfully updated.");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString()); // Log the exception details
        return Results.Problem(detail: ex.Message, title: "An error occurred while updating the location", statusCode: 500);
    }
})
.WithName("UpdateVozacLokacija");

app.MapGet("/api/Vozac/Lokacija/{id}", (int id) =>
{
    try
    {
        string lokacijaJson = library.VratiLokacijuPoId(id);
        var lokacija = JsonConvert.DeserializeObject<Dictionary<string, double?>>(lokacijaJson);
        if (lokacija["Latitude"].HasValue && lokacija["Longitude"].HasValue)
        {
            return Results.Ok(lokacija);
        }
        else
        {
            return Results.NotFound("Lokacija nije pronađena.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching location: {ex.ToString()}");
        return Results.Problem(detail: ex.Message, title: "An error occurred while fetching the location", statusCode: 500);
    }
})
.WithName("VratiLokacijuPoId");

app.MapPost("/api/Vozac/ProveraLogin", (string username, string password) =>
{
    int userId = library.ProveraLoginVozac(username, password);
    if (userId > 0)
    {
        DataTable vozac = library.VratiVozacaPoId(userId);
        if (vozac.Rows.Count > 0)
        {
            string jsonResult = library.DataTableToJson(vozac);
            return Results.Ok(jsonResult);
        }
        else
        {
            return Results.NotFound("Vozač nije pronađen.");
        }
    }
    else
    {
        return Results.BadRequest("Invalid username or password.");
    }
}).WithName("ProveraLoginVozac");

app.MapGet("/api/Vozac/{id}", (int id) =>
{
    try
    {
        // Dohvatanje podataka o vozaču
        DataTable vozac = library.VratiVozacaPoId(id);
        if (vozac.Rows.Count > 0)
        {
            DataRow vozacRow = vozac.Rows[0];

            // Dohvatanje dodatnih informacija
            int brojVoznji = library.BrojVoznjiPoVozacId(id);
            decimal ukupnaZarada = library.UkupnaZaradaPoVozacId(id);
            int totalOcena = library.UkupanBrojOcenaPoVozacId(id);

            // Dohvatanje ponuda po vozaču
            DataTable ponudeDt = library.VratiPonudePoVozacId(id);
            var ponude = new List<object>();

            if (ponudeDt != null && ponudeDt.Rows.Count > 0)
            {
                foreach (DataRow row in ponudeDt.Rows)
                {
                    var ponudaObj = new Dictionary<string, object>();

                    if (row.Table.Columns.Contains("cena") && !row.IsNull("cena"))
                    {
                        ponudaObj["Cena"] = Convert.ToDecimal(row["cena"]);
                    }
                    if (row.Table.Columns.Contains("datum_isporuke") && !row.IsNull("datum_isporuke"))
                    {
                        ponudaObj["DatumIsporuke"] = Convert.ToDateTime(row["datum_isporuke"]).ToString("yyyy-MM-dd");
                    }

                    if (ponudaObj.Count > 0)
                    {
                        ponude.Add(ponudaObj);
                    }
                }
            }

            // Kreiranje rezultata sa dodatnim poljima
            var vozacObj = new Dictionary<string, object>();

            if (vozacRow.Table.Columns.Contains("id") && !vozacRow.IsNull("id")) vozacObj["id"] = Convert.ToInt32(vozacRow["id"]);
            if (vozacRow.Table.Columns.Contains("username") && !vozacRow.IsNull("username")) vozacObj["username"] = vozacRow["username"].ToString();
            if (vozacRow.Table.Columns.Contains("email") && !vozacRow.IsNull("email")) vozacObj["email"] = vozacRow["email"].ToString();
            if (vozacRow.Table.Columns.Contains("ime") && !vozacRow.IsNull("ime")) vozacObj["ime"] = vozacRow["ime"].ToString();
            if (vozacRow.Table.Columns.Contains("vozilo") && !vozacRow.IsNull("vozilo")) vozacObj["vozilo"] = vozacRow["vozilo"].ToString();
            if (vozacRow.Table.Columns.Contains("registracija") && !vozacRow.IsNull("registracija")) vozacObj["registracija"] = vozacRow["registracija"].ToString();
            if (vozacRow.Table.Columns.Contains("slika") && !vozacRow.IsNull("slika")) vozacObj["slika"] = vozacRow["slika"].ToString();
            if (vozacRow.Table.Columns.Contains("ocena") && !vozacRow.IsNull("ocena")) vozacObj["ocena"] = Convert.ToDecimal(vozacRow["ocena"]);
            if (brojVoznji >= 0) vozacObj["brojVoznji"] = brojVoznji;
            if (ukupnaZarada >= 0) vozacObj["ukupnaZarada"] = ukupnaZarada;
            if (totalOcena >= 0) vozacObj["ukupanBrojOcena"] = totalOcena;
            if (ponude.Count > 0) vozacObj["ponude"] = ponude;

            return Results.Ok(vozacObj);
        }
        else
        {
            return Results.NotFound("Vozač nije pronađen.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching driver: {ex.ToString()}"); // Log the exception details
        return Results.Problem(detail: ex.Message, title: "An error occurred while fetching the driver", statusCode: 500);
    }
})
.WithName("VratiVozacaPoId");

app.MapPost("/api/OcenaVozaca/Dodaj", (int vozacId, int poslodavacId, int ocena, string komentar) =>
{
    try
    {
        library.DodajOcenuVozaca(vozacId, poslodavacId, ocena, komentar);
        return Results.Ok("Ocena successfully added.");
    }
    catch (Exception ex)
    {
        // Log the exception details (this is an example, you should log it properly in production)
        Console.WriteLine(ex.ToString());

        // Return a detailed error response
        return Results.Problem(detail: ex.Message, title: "An error occurred while adding the rating", statusCode: 500);
    }
})
.WithName("DodajOcenuVozaca");
app.MapDelete("/api/OcenaVozaca", (int id) =>
{
    library.ObrisiOcenuVozaca(id);
    return Results.Ok("Ocena successfully deleted.");
})
.WithName("ObrisiOcenuVozaca");

app.MapGet("/api/OcenaVozaca/Vozac/{vozacId}", async (int vozacId) =>
{
    DataTable oceneDt = library.PrikaziOcenePoVozacId(vozacId);

    if (oceneDt.Rows.Count > 0)
    {
        var resultList = new List<object>();

        foreach (DataRow row in oceneDt.Rows)
        {
            int poslodavacId = Convert.ToInt32(row["poslodavac_id"]);
            DataTable poslodavacDt = library.VratiPoslodavcaPoId(poslodavacId);
            DataTable vozacDt = library.VratiVozacaPoId(vozacId);

            var ocenaObj = new
            {
                ocena = row["ocena"],
                komentar = row["komentar"],
                poslodavac = poslodavacDt.Rows.Count > 0 ? library.DataRowToDictionary(poslodavacDt.Rows[0]) : null,
                vozac = vozacDt.Rows.Count > 0 ? library.DataRowToDictionary(vozacDt.Rows[0]) : null
            };

            resultList.Add(ocenaObj);
        }

        return Results.Ok(resultList);
    }
    else
    {
        return Results.NotFound("Vozač ili ocene nisu pronađene.");
    }
})
.WithName("PrikaziOcenePoVozacId");

app.MapGet("/api/OcenaVozaca/Poslodavac/{poslodavacId}", async (int poslodavacId) =>
{
    DataTable oceneDt = library.PrikaziOcenePoPoslodavacId(poslodavacId);

    if (oceneDt.Rows.Count > 0)
    {
        var resultList = new List<object>();

        foreach (DataRow row in oceneDt.Rows)
        {
            int vozacId = Convert.ToInt32(row["vozac_id"]);
            DataTable poslodavacDt = library.VratiPoslodavcaPoId(poslodavacId);
            DataTable vozacDt = library.VratiVozacaPoId(vozacId);

            var ocenaObj = new
            {
                ocena = row["ocena"],
                komentar = row["komentar"],
                poslodavac = poslodavacDt.Rows.Count > 0 ? library.DataRowToDictionary(poslodavacDt.Rows[0]) : null,
                vozac = vozacDt.Rows.Count > 0 ? library.DataRowToDictionary(vozacDt.Rows[0]) : null
            };

            resultList.Add(ocenaObj);
        }

        return Results.Ok(resultList);
    }
    else
    {
        return Results.NotFound("Poslodavac ili ocene nisu pronađene.");
    }
})
.WithName("PrikaziOcenePoPoslodavacId");


//Ponuda---------------------------------------------------------------------------------------

app.MapPost("/api/Ponuda/Dodaj", (int poslodavac_id, string naziv_tereta, string vrsta_tereta, decimal tezina_tereta, string dimenzije_tereta, string mestoPolaska, string mestoIsporuke, DateTime datum_polaska, DateTime datum_isporuke, string dodatan_opis, decimal cena) =>
{
    

    library.DodajPonudu(
        poslodavac_id, naziv_tereta, vrsta_tereta,
        tezina_tereta, dimenzije_tereta, mestoPolaska, mestoIsporuke,
        datum_polaska, datum_isporuke, dodatan_opis, cena
    );
    return Results.Ok("Ponuda je uspešno dodata.");
})
.WithName("DodajPonudu");

app.MapDelete("/api/Ponuda", (int id) =>
{
    library.ObrisiPonuduPoId(id);
    return Results.Ok("Ponuda je uspešno obrisana.");
})
.WithName("ObrisiPonuduPoId");

app.MapGet("/api/Ponuda/PrikaziSve", () =>
{
    var ponude = library.PrikaziSvePonude();
    return Results.Ok(ponude);
})
.WithName("PrikaziSvePonude");

app.MapGet("/api/Ponuda/PrikaziBezVozaca", () =>
{
    try
    {
        var ponude = library.PrikaziPonudeBezVozaca();
        return Results.Ok(ponude);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching offers without driver: {ex.ToString()}"); // Log the exception details
        return Results.Problem(detail: ex.Message, title: "An error occurred while fetching offers without driver", statusCode: 500);
    }
})
.WithName("PrikaziPonudeBezVozaca");




app.MapGet("/api/Ponuda/{id}", async (int id) =>
{
    DataTable ponuda = await library.VratiPonuduPoIdAsync(id);
    if (ponuda.Rows.Count > 0)
    {
        DataRow ponudaRow = ponuda.Rows[0];

        // Dohvati podatke o poslodavcu
        int poslodavacId = Convert.ToInt32(ponudaRow["poslodavac_id"]);
        DataTable poslodavacDt = library.VratiPoslodavcaPoId(poslodavacId);
        var poslodavac = poslodavacDt.Rows.Count > 0 ? library.DataRowToDictionary(poslodavacDt.Rows[0]) : null;

        // Dohvati podatke o vozaču ako postoji vozac_id
        var vozac = (object)null;
        if (ponudaRow["vozac_id"] != DBNull.Value)
        {
            int vozacId = Convert.ToInt32(ponudaRow["vozac_id"]);
            DataTable vozacDt = library.VratiVozacaPoId(vozacId);
            vozac = vozacDt.Rows.Count > 0 ? library.DataRowToDictionary(vozacDt.Rows[0]) : null;
        }

        // Kreiraj rezultat
        var ponudaObj = new
        {
            id = ponudaRow["id"],
            poslodavac = poslodavac,
            vozac = vozac,
            naziv_tereta = ponudaRow["naziv_tereta"],
            vrsta_tereta = ponudaRow["vrsta_tereta"],
            tezina_tereta = ponudaRow["tezina_tereta"],
            dimenzije_tereta = ponudaRow["dimenzije_tereta"],
            mesto_polaska = ponudaRow["mesto_polaska"],
            mesto_isporuke = ponudaRow["mesto_isporuke"],
            datum_polaska = ponudaRow["datum_polaska"],
            datum_isporuke = ponudaRow["datum_isporuke"],
            trajanje_puta = ponudaRow["trajanje_puta"],
            dodatan_opis = ponudaRow["dodatan_opis"],
            cena = ponudaRow["cena"],
            razdaljina = ponudaRow["razdaljina"],
            mapa = ponudaRow["mapa"]
        };

        return Results.Ok(ponudaObj);
    }
    else
    {
        return Results.NotFound("Ponuda nije pronađena.");
    }
}).WithName("VratiPonuduPoId");

app.MapGet("/api/Ponuda/PoslodavacID", (int poslodavac_id) =>
{
    var ponude = library.PrikaziPonudePoPoslodavacID(poslodavac_id);
    return Results.Ok(ponude);
})
.WithName("PrikaziPonudePoPoslodavacID");
app.MapGet("/api/Ponuda/VozacId", (int vozacId) =>
{
    DataTable ponude = library.VratiPonudePoVozacId(vozacId);
    if (ponude.Rows.Count > 0)
    {
        string jsonResult = library.DataTableToJson(ponude);
        return Results.Ok(jsonResult);
    }
    else
    {
        return Results.NotFound("Ponude nisu pronađene za datog vozača.");
    }
})
.WithName("VratiPonudePoVozacId");

app.MapPut("/api/Ponuda/VozacId", (int ponudaId, int vozacId) =>
{
    library.AzurirajVozacId(ponudaId, vozacId);
    return Results.Ok("Vozač je uspešno ažuriran u ponudi.");
})
.WithName("AzurirajVozacId");

app.MapDelete("/api/Ponuda/VozacId", (int ponudaId) =>
{
    library.ObrisiVozacId(ponudaId);
    return Results.Ok("Vozač je uspešno obrisan iz ponude.");
})
.WithName("ObrisiVozacId");



app.Run();
