using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Types;
using Newtonsoft.Json;


namespace KamioniApi
{
    public class Library
    {

        public string connectionString;
        SqlConnection conn;
        SqlCommand comm;

        public Library(string connString)
        {
            connectionString = connString;
        }


        public Dictionary<string, object> DataRowToDictionary(DataRow row)
        {
            var dictionary = new Dictionary<string, object>();
            foreach (DataColumn column in row.Table.Columns)
            {
                dictionary[column.ColumnName] = row[column];
            }
            return dictionary;
        }



        public string DataTableToJson(DataTable table)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include
            };

            var rows = new List<Dictionary<string, object>>();

            foreach (DataRow dr in table.Rows)
            {
                var row = new Dictionary<string, object>();
                foreach (DataColumn col in table.Columns)
                {
                    if (col.DataType == typeof(SqlGeography))
                    {
                        var geography = dr[col] as SqlGeography;
                        row[col.ColumnName] = geography != null ? geography.ToString() : null;
                    }
                    else
                    {
                        row[col.ColumnName] = dr[col] == DBNull.Value ? null : dr[col];
                    }
                }
                rows.Add(row);
            }

            return JsonConvert.SerializeObject(rows, settings);
        }

        //GOOGLE API MAPS--------------------------------------------------------------------------
        public async Task<string> GetDistanceAsync(string origin, string destination, string apiKey)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={origin}&destinations={destination}&key={apiKey}";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(responseBody);
                if (json.status == "OK" && json.rows[0].elements[0].status == "OK")
                {
                    return json.rows[0].elements[0].distance.text;
                }
                else
                {
                    throw new Exception("Error fetching distance from Google Maps API");
                }
            }
        }


        private string GenerateMapHtml(string origin, string destination, string apiKey)
        {
            string url = $"https://www.google.com/maps/embed/v1/directions?key={apiKey}&origin={origin}&destination={destination}";
            string html = $"<iframe width=\"600\" height=\"450\" frameborder=\"0\" style=\"border:0\" src=\"{url}\" allowfullscreen></iframe>";
            return html;
        }

        //Zahtev Za Ponudu--------------------------------------------------------------------------------
        public void DodajZahtevZaPonudu(int vozacId, int poslodavacId, int ponudaId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("DodajZahtevZaPonudu", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@vozac_id", vozacId);
                    comm.Parameters.AddWithValue("@poslodavac_id", poslodavacId);
                    comm.Parameters.AddWithValue("@ponuda_id", ponudaId);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }
        public void ObrisiZahtevZaPonudu(int id)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("ObrisiZahtevZaPonudu", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }


        public List<Dictionary<string, object>> PrikaziZahtevePoPoslodavacId(int poslodavacId)
        {
            var zahtevi = new List<Dictionary<string, object>>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("PrikaziZahtevePoPoslodavacId", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@poslodavac_id", poslodavacId);

                    conn.Open();
                    using (SqlDataReader reader = comm.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var zahtev = new Dictionary<string, object>();

                            if (!reader.IsDBNull(0))
                                zahtev["ZahtevId"] = reader.GetInt32(0);
                            if (!reader.IsDBNull(1))
                                zahtev["VozacId"] = reader.GetInt32(1);
                            if (!reader.IsDBNull(2))
                                zahtev["VozacUsername"] = reader.GetString(2);
                            if (!reader.IsDBNull(3))
                                zahtev["VozacEmail"] = reader.GetString(3);
                            if (!reader.IsDBNull(4))
                                zahtev["VozacIme"] = reader.GetString(4);
                            if (!reader.IsDBNull(5))
                                zahtev["VozacVozilo"] = reader.GetString(5);
                            if (!reader.IsDBNull(6))
                                zahtev["VozacRegistracija"] = reader.GetString(6);
                            if (!reader.IsDBNull(7))
                                zahtev["VozacSlika"] = reader["VozacSlika"];
                            if (!reader.IsDBNull(8))
                                zahtev["VozacOcena"] = reader.GetDecimal(8);
                            if (!reader.IsDBNull(9))
                                zahtev["PoslodavacId"] = reader.GetInt32(9);
                            if (!reader.IsDBNull(10))
                                zahtev["PonudaId"] = reader.GetInt32(10);
                            if (!reader.IsDBNull(11))
                                zahtev["NazivTereta"] = reader.GetString(11);
                            if (!reader.IsDBNull(12))
                                zahtev["VrstaTereta"] = reader.GetString(12);
                            if (!reader.IsDBNull(13))
                                zahtev["TezinaTereta"] = reader.GetDecimal(13);
                            if (!reader.IsDBNull(14))
                                zahtev["DimenzijeTereta"] = reader.GetString(14);
                            if (!reader.IsDBNull(15))
                                zahtev["MestoPolaska"] = reader.GetString(15);
                            if (!reader.IsDBNull(16))
                                zahtev["MestoIsporuke"] = reader.GetString(16);
                            if (!reader.IsDBNull(17))
                                zahtev["DatumPolaska"] = reader.GetDateTime(17);
                            if (!reader.IsDBNull(18))
                                zahtev["DatumIsporuke"] = reader.GetDateTime(18);
                            if (!reader.IsDBNull(19))
                                zahtev["TrajanjePuta"] = reader.GetInt32(19);
                            if (!reader.IsDBNull(20))
                                zahtev["DodatanOpis"] = reader.GetString(20);
                            if (!reader.IsDBNull(21))
                                zahtev["Cena"] = reader.GetDecimal(21);

                            zahtevi.Add(zahtev);
                        }
                    }
                }
            }

            return zahtevi;
        }


        //POSLODAVAC--------------------------------------------------------------------------------------
        public void DodajPoslodavca(string username, string password, string email, string naziv, string telefon)
        {
            conn = new SqlConnection(connectionString);
            comm = new SqlCommand();
            comm.CommandType = CommandType.StoredProcedure;
            comm.CommandText = "DodajPoslodavac";
            comm.Connection = conn;
            comm.Parameters.AddWithValue("@username", username);
            comm.Parameters.AddWithValue("@password", password);
            comm.Parameters.AddWithValue("@email", email);
            comm.Parameters.AddWithValue("@naziv", naziv);
            comm.Parameters.AddWithValue("@telefon", telefon);

            conn.Open();
            comm.ExecuteNonQuery();
            conn.Close();
        }

        public void UpdatePoslodavacUsername(int id, string username)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("UpdatePoslodavacUsername", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);
                    comm.Parameters.AddWithValue("@username", username);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public void UpdatePoslodavacPassword(int id, string password)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("UpdatePoslodavacPassword", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);
                    comm.Parameters.AddWithValue("@password", password);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public void UpdatePoslodavacEmail(int id, string email)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("UpdatePoslodavacEmail", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);
                    comm.Parameters.AddWithValue("@email", email);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public void UpdatePoslodavacNaziv(int id, string naziv)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("UpdatePoslodavacNaziv", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);
                    comm.Parameters.AddWithValue("@naziv", naziv);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public void UpdatePoslodavacTelefon(int id, string telefon)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("UpdatePoslodavacTelefon", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);
                    comm.Parameters.AddWithValue("@telefon", telefon);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }


        public int ProveraLoginPoslodavac(string username, string password)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("ProveraLoginPoslodavac", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;

                    comm.Parameters.AddWithValue("@username", username);
                    comm.Parameters.AddWithValue("@password", password);

                    SqlParameter idParam = new SqlParameter("@id", SqlDbType.Int);
                    idParam.Direction = ParameterDirection.Output;
                    comm.Parameters.Add(idParam);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();

                    return (int)idParam.Value;
                }
            }
        }

        public bool ProveraUsernamePoslodavac(string username)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("ProveraUsernamePoslodavac", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;

                    comm.Parameters.AddWithValue("@username", username);

                    SqlParameter existsParam = new SqlParameter("@exists", SqlDbType.Int);
                    existsParam.Direction = ParameterDirection.Output;
                    comm.Parameters.Add(existsParam);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();

                    return (int)existsParam.Value == 1;
                }
            }
        }

        public DataTable VratiPoslodavcaPoId(int id)
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("VratiPoslodavcaPoId", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);

                    SqlDataAdapter da = new SqlDataAdapter(comm);

                    conn.Open();
                    da.Fill(dt);
                    conn.Close();
                }
            }

            return dt;
        }

        public int UkupanBrojOcenaPoPoslodavacId(int poslodavacId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("UkupanBrojOcenaPoPoslodavacId", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@poslodavac_id", poslodavacId);

                    SqlParameter totalOcenaParam = new SqlParameter("@totalOcena", SqlDbType.Int);
                    totalOcenaParam.Direction = ParameterDirection.Output;
                    comm.Parameters.Add(totalOcenaParam);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();

                    return (int)totalOcenaParam.Value;
                }
            }
        }

        public DataTable PrikaziOcenePoslodavcaPoPoslodavacId(int poslodavacId)
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("PrikaziOcenePoslodavcaPoPoslodavacId", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@poslodavac_id", poslodavacId);

                    SqlDataAdapter da = new SqlDataAdapter(comm);

                    conn.Open();
                    da.Fill(dt);
                    conn.Close();
                }
            }

            return dt;
        }

        public DataTable PrikaziOcenePoslodavcaPoVozacId(int vozacId)
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("PrikaziOcenePoslodavcaPoVozacId", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@vozac_id", vozacId);

                    SqlDataAdapter da = new SqlDataAdapter(comm);

                    conn.Open();
                    da.Fill(dt);
                    conn.Close();
                }
            }

            return dt;
        }


        public void ObrisiOcenuPoslodavca(int id)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("ObrisiOcenuPoslodavca", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public void DodajOcenuPoslodavca(int poslodavacId, int vozacId, int ocena, string komentar)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("DodajOcenuPoslodavca", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@poslodavac_id", poslodavacId);
                    comm.Parameters.AddWithValue("@vozac_id", vozacId);
                    comm.Parameters.AddWithValue("@ocena", ocena);
                    comm.Parameters.AddWithValue("@komentar", komentar);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }


        //*VOZAC-------------------------------------------------------------------------------------
        public void DodajVozaca(string username, string password, string email, string ime)
        {
            conn = new SqlConnection(connectionString);
            comm = new SqlCommand();
            comm.CommandType = CommandType.StoredProcedure;
            comm.CommandText = "DodajVozac";
            comm.Connection = conn;
            comm.Parameters.AddWithValue("@username", username);
            comm.Parameters.AddWithValue("@password", password);
            comm.Parameters.AddWithValue("@email", email);
            comm.Parameters.AddWithValue("@ime", ime);

            conn.Open();
            comm.ExecuteNonQuery();
            conn.Close();
        }

        public int ProveraLoginVozac(string username, string password)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("ProveraLoginVozac", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;

                    comm.Parameters.AddWithValue("@username", username);
                    comm.Parameters.AddWithValue("@password", password);

                    SqlParameter idParam = new SqlParameter("@id", SqlDbType.Int);
                    idParam.Direction = ParameterDirection.Output;
                    comm.Parameters.Add(idParam);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();

                    return (int)idParam.Value;
                }
            }
        }

        public bool ProveraUsernameVozac(string username)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("ProveraUsernameVozac", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;

                    comm.Parameters.AddWithValue("@username", username);

                    SqlParameter existsParam = new SqlParameter("@exists", SqlDbType.Int);
                    existsParam.Direction = ParameterDirection.Output;
                    comm.Parameters.Add(existsParam);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();

                    return (int)existsParam.Value == 1;
                }
            }
        }

        public DataTable VratiVozacaPoId(int id)
        {
            DataTable dt = new DataTable();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand comm = new SqlCommand("VratiVozacaPoId", conn))
                    {
                        comm.CommandType = CommandType.StoredProcedure;
                        comm.Parameters.AddWithValue("@id", id);

                        SqlDataAdapter da = new SqlDataAdapter(comm);
                        conn.Open();
                        da.Fill(dt);

                        // Process SqlGeography columns
                        foreach (DataRow row in dt.Rows)
                        {
                            foreach (DataColumn col in dt.Columns)
                            {
                                if (col.DataType == typeof(SqlGeography))
                                {
                                    
                                    if (row[col] as SqlGeography != null)
                                    {
                                        SqlGeography geography = row[col] as SqlGeography;
                                        row[col] = geography.ToString();
                                    }
                                }
                            }
                        }

                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing SQL command: {ex.ToString()}");
                throw; // Re-throw the exception to be caught by the calling method
            }

            return dt;
        }

        public string VratiLokacijuPoId(int id)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("VratiLokacijuPoId", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);

                    conn.Open();

                    using (SqlDataReader reader = comm.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            double? latitude = reader.IsDBNull(0) ? (double?)null : reader.GetDouble(0);
                            double? longitude = reader.IsDBNull(1) ? (double?)null : reader.GetDouble(1);
                            var result = new { Latitude = latitude, Longitude = longitude };
                            return JsonConvert.SerializeObject(result);
                        }
                    }

                    conn.Close();
                }
            }

            return JsonConvert.SerializeObject(new { Latitude = (double?)null, Longitude = (double?)null });
        }
    


    public void UpdateVozacUsername(int id, string username)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("UpdateVozacUsername", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);
                    comm.Parameters.AddWithValue("@username", username);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public void UpdateVozacPassword(int id, string password)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("UpdateVozacPassword", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);
                    comm.Parameters.AddWithValue("@password", password);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public void UpdateVozacEmail(int id, string email)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("UpdateVozacEmail", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);
                    comm.Parameters.AddWithValue("@email", email);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public void UpdateVozacVozilo(int id, string vozilo)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("UpdateVozacVozilo", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);
                    comm.Parameters.AddWithValue("@vozilo", vozilo);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }



        public void UpdateVozacIme(int id, string ime)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("UpdateVozacIme", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);
                    comm.Parameters.AddWithValue("@ime", ime);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public void UpdateVozacRegistracija(int id, string registracija)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("UpdateVozacRegistracija", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);
                    comm.Parameters.AddWithValue("@registracija", registracija);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public void UpdateVozacSlika(int id, byte[] slika)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("UpdateVozacSlika", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);
                    comm.Parameters.AddWithValue("@slika", slika);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public void UpdateVozacLokacija(int id, double latitude, double longitude)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("UpdateVozacLokacija", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);
                    comm.Parameters.AddWithValue("@latitude", latitude);
                    comm.Parameters.AddWithValue("@longitude", longitude);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public void DodajOcenuVozaca(int vozacId, int poslodavacId, int ocena, string komentar)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("DodajOcenuVozaca", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@vozac_id", vozacId);
                    comm.Parameters.AddWithValue("@poslodavac_id", poslodavacId);
                    comm.Parameters.AddWithValue("@ocena", ocena);
                    comm.Parameters.AddWithValue("@komentar", komentar);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public DataTable PrikaziOcenePoVozacId(int vozacId)
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("PrikaziOcenePoVozacId", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@vozac_id", vozacId);

                    SqlDataAdapter da = new SqlDataAdapter(comm);

                    conn.Open();
                    da.Fill(dt);
                    conn.Close();
                }
            }

            return dt;
        }

        public DataTable PrikaziOcenePoPoslodavacId(int poslodavacId)
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("PrikaziOcenePoPoslodavacId", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@poslodavac_id", poslodavacId);

                    SqlDataAdapter da = new SqlDataAdapter(comm);

                    conn.Open();
                    da.Fill(dt);
                    conn.Close();
                }
            }

            return dt;
        }

        public int UkupanBrojOcenaPoVozacId(int vozacId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("UkupanBrojOcenaPoVozacId", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@vozac_id", vozacId);

                    SqlParameter totalOcenaParam = new SqlParameter("@totalOcena", SqlDbType.Int);
                    totalOcenaParam.Direction = ParameterDirection.Output;
                    comm.Parameters.Add(totalOcenaParam);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();

                    return (int)totalOcenaParam.Value;
                }
            }
        }

        public void ObrisiOcenuVozaca(int id)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("ObrisiOcenuVozaca", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public int BrojVoznjiPoVozacId(int vozacId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("BrojVoznjiPoVozacId", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@vozac_id", vozacId);

                    SqlParameter brojVoznjiParam = new SqlParameter("@brojVoznji", SqlDbType.Int);
                    brojVoznjiParam.Direction = ParameterDirection.Output;
                    comm.Parameters.Add(brojVoznjiParam);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();

                    return (int)brojVoznjiParam.Value;
                }
            }
        }

        public decimal UkupnaZaradaPoVozacId(int vozacId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("UkupnaZaradaPoVozacId", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@vozac_id", vozacId);

                    SqlParameter ukupnaZaradaParam = new SqlParameter("@ukupnaZarada", SqlDbType.Decimal);
                    ukupnaZaradaParam.Direction = ParameterDirection.Output;
                    ukupnaZaradaParam.Precision = 18; // Postavljanje preciznosti, npr. 18
                    ukupnaZaradaParam.Scale = 2; // Postavljanje skale, npr. 2 decimalna mesta
                    comm.Parameters.Add(ukupnaZaradaParam);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();

                    // Proveravamo da li je izlazna vrednost `DBNull`
                    if (ukupnaZaradaParam.Value == DBNull.Value)
                    {
                        return 0; // Ili neka druga podrazumevana vrednost
                    }

                    return (decimal)ukupnaZaradaParam.Value;
                }
            }
        }



        /*PONUDE-------------------------------------------------------------------- */


        public void DodajPonudu(int poslodavac_id, string naziv_tereta, string vrsta_tereta, decimal tezina_tereta, string dimenzije_tereta, string mesto_polaska, string mesto_isporuke, DateTime datum_polaska, DateTime datum_isporuke, string dodatan_opis, decimal cena)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("DodajPonuda", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@poslodavac_id", poslodavac_id);
                    comm.Parameters.AddWithValue("@naziv_tereta", naziv_tereta);
                    comm.Parameters.AddWithValue("@vrsta_tereta", vrsta_tereta);
                    comm.Parameters.AddWithValue("@tezina_tereta", tezina_tereta);
                    comm.Parameters.AddWithValue("@dimenzije_tereta", dimenzije_tereta);
                    comm.Parameters.AddWithValue("@mesto_polaska", mesto_polaska);
                    comm.Parameters.AddWithValue("@mesto_isporuke", mesto_isporuke);
                    comm.Parameters.AddWithValue("@datum_polaska", datum_polaska);
                    comm.Parameters.AddWithValue("@datum_isporuke", datum_isporuke);
                    comm.Parameters.AddWithValue("@dodatan_opis", dodatan_opis);
                    comm.Parameters.AddWithValue("@cena", cena);

                    conn.Open();
                    comm.ExecuteNonQuery();
                }
            }
        }

        public void ObrisiPonuduPoId(int id)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("ObrisiPonuduPoId", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public async Task<DataTable> VratiPonuduPoIdAsync(int id)
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("VratiPonuduPoId", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@id", id);

                    SqlDataAdapter da = new SqlDataAdapter(comm);

                    conn.Open();
                    da.Fill(dt);
                    conn.Close();
                }
            }

            if (dt.Rows.Count > 0)
            {
                string mestoPolaska = dt.Rows[0]["mesto_polaska"].ToString();
                string mestoIsporuke = dt.Rows[0]["mesto_isporuke"].ToString();

                // Zameni sa vašim Google Maps API ključem
                string apiKey = "AIzaSyDzRtEnNuuvXMNM8t--5uh3d7uXgqH2gsE";
                string distance = await GetDistanceAsync(mestoPolaska, mestoIsporuke, apiKey);

              
                string mapHtml = GenerateMapHtml(mestoPolaska, mestoIsporuke, apiKey);

                DataColumn distanceColumn = new DataColumn("razdaljina", typeof(string));
                DataColumn mapHtmlColumn = new DataColumn("mapa", typeof(string));
                dt.Columns.Add(distanceColumn);
                dt.Columns.Add(mapHtmlColumn);

                foreach (DataRow row in dt.Rows)
                {
                    row["razdaljina"] = distance;
                    row["mapa"] = mapHtml;
                }
            }

            return dt;
        }
        public void AzurirajVozacId(int ponudaId, int vozacId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Pokušaj brisanja svih zahteva za ponudu sa datim ponuda_id
                try
                {
                    using (SqlCommand comm = new SqlCommand("ObrisiSveZahtevePoPonudaId", conn))
                    {
                        comm.CommandType = CommandType.StoredProcedure;
                        comm.Parameters.AddWithValue("@ponuda_id", ponudaId);
                        comm.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    // Logovanje greške ako je potrebno, ali funkcija za ažuriranje će se i dalje izvršiti
                    Console.WriteLine($"Error deleting requests for offer: {ex.Message}");
                }

                // Ažuriranje vozac_id u ponudi
                using (SqlCommand comm = new SqlCommand("AzurirajVozacId", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@ponuda_id", ponudaId);
                    comm.Parameters.AddWithValue("@vozac_id", vozacId);
                    comm.ExecuteNonQuery();
                }
            }
        }

        public void ObrisiVozacId(int ponudaId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("ObrisiVozacId", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;

                    comm.Parameters.AddWithValue("@ponuda_id", ponudaId);

                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public DataTable VratiPonudePoVozacId(int vozacId)
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("PrikaziPonudePoVozacID", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@vozac_id", vozacId);

                    SqlDataAdapter da = new SqlDataAdapter(comm);

                    conn.Open();
                    da.Fill(dt);
                    conn.Close();
                }
            }

            return dt;
        }

        public List<Dictionary<string, object>> PrikaziSvePonude()
{
    var ponude = new List<Dictionary<string, object>>();

    using (SqlConnection conn = new SqlConnection(connectionString))
    {
        using (SqlCommand comm = new SqlCommand("PrikaziSvePonude", conn))
        {
            comm.CommandType = CommandType.StoredProcedure;
            conn.Open();

            using (SqlDataReader reader = comm.ExecuteReader())
            {
                while (reader.Read())
                {
                    var ponuda = new Dictionary<string, object>();

                    if (!reader.IsDBNull(0))
                        ponuda["Id"] = reader.GetInt32(0);
                    if (!reader.IsDBNull(1))
                        ponuda["PoslodavacId"] = reader.GetInt32(1);
                    if (!reader.IsDBNull(2))
                        ponuda["VozacId"] = reader.GetInt32(2);
                    if (!reader.IsDBNull(3))
                        ponuda["NazivTereta"] = reader.GetString(3);
                    if (!reader.IsDBNull(4))
                        ponuda["VrstaTereta"] = reader.GetString(4);
                    if (!reader.IsDBNull(5))
                        ponuda["TezinaTereta"] = reader.GetDecimal(5);
                    if (!reader.IsDBNull(6))
                        ponuda["DimenzijeTereta"] = reader.GetString(6);
                    
                    if (!reader.IsDBNull(7))
                        ponuda["DatumPolaska"] = reader.GetDateTime(7);
                    if (!reader.IsDBNull(8))
                        ponuda["DatumIsporuke"] = reader.GetDateTime(8);
                    if (!reader.IsDBNull(9))
                        ponuda["TrajanjePuta"] = reader.GetInt32(9);
                    if (!reader.IsDBNull(10))
                        ponuda["DodatanOpis"] = reader.GetString(10);
                    if (!reader.IsDBNull(11))
                        ponuda["Cena"] = reader.GetDecimal(11);
                            if (!reader.IsDBNull(12))
                                ponuda["MestoPolaska"] = reader.GetString(12);
                            if (!reader.IsDBNull(13))
                                ponuda["MestoIsporuke"] = reader.GetString(13);

                            ponude.Add(ponuda);
                }
            }
        }
    }

    return ponude;
}

        public List<Dictionary<string, object>> PrikaziPonudeBezVozaca()
        {
            var ponude = new List<Dictionary<string, object>>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("PrikaziPonudeBezVozaca", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    conn.Open();

                    using (SqlDataReader reader = comm.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var ponuda = new Dictionary<string, object>();

                            if (!reader.IsDBNull(0))
                                ponuda["Id"] = reader.GetInt32(0);
                            if (!reader.IsDBNull(1))
                                ponuda["PoslodavacId"] = reader.GetInt32(1);
                            if (!reader.IsDBNull(2))
                                ponuda["VozacId"] = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2);
                            if (!reader.IsDBNull(3))
                                ponuda["NazivTereta"] = reader.GetString(3);
                            if (!reader.IsDBNull(4))
                                ponuda["VrstaTereta"] = reader.GetString(4);
                            if (!reader.IsDBNull(5))
                                ponuda["TezinaTereta"] = reader.GetDecimal(5);
                            if (!reader.IsDBNull(6))
                                ponuda["DimenzijeTereta"] = reader.GetString(6);

                            if (!reader.IsDBNull(7))
                                ponuda["DatumPolaska"] = reader.GetDateTime(7).ToString("yyyy-MM-dd HH:mm:ss");
                            if (!reader.IsDBNull(8))
                                ponuda["DatumIsporuke"] = reader.GetDateTime(8).ToString("yyyy-MM-dd HH:mm:ss");
                            if (!reader.IsDBNull(9))
                                ponuda["TrajanjePuta"] = reader.GetInt32(9);
                            if (!reader.IsDBNull(10))
                                ponuda["DodatanOpis"] = reader.GetString(10);
                            if (!reader.IsDBNull(11))
                                ponuda["Cena"] = reader.GetDecimal(11);
                            if (!reader.IsDBNull(12))
                                ponuda["MestoPolaska"] = reader.GetString(12);
                            if (!reader.IsDBNull(13))
                                ponuda["MestoIsporuke"] = reader.GetString(13);
                            if (!reader.IsDBNull(14))
                                ponuda["DatumKreiranja"] = reader.GetDateTime(14).ToString("yyyy-MM-dd HH:mm:ss");

                            ponude.Add(ponuda);
                        }
                    }
                }
            }

            return ponude;
        }
        public List<Dictionary<string, object>> PrikaziPonudePoPoslodavacID(int poslodavacId)
        {
            var ponude = new List<Dictionary<string, object>>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand comm = new SqlCommand("PrikaziPonudePoPoslodavacID", conn))
                {
                    comm.CommandType = CommandType.StoredProcedure;
                    comm.Parameters.AddWithValue("@poslodavac_id", poslodavacId);
                    conn.Open();

                    using (SqlDataReader reader = comm.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var ponuda = new Dictionary<string, object>();

                            if (!reader.IsDBNull(0))
                                ponuda["Id"] = reader.GetInt32(0);
                            if (!reader.IsDBNull(1))
                                ponuda["PoslodavacId"] = reader.GetInt32(1);
                            if (!reader.IsDBNull(2))
                                ponuda["VozacId"] = reader.GetInt32(2);
                            if (!reader.IsDBNull(3))
                                ponuda["NazivTereta"] = reader.GetString(3);
                            if (!reader.IsDBNull(4))
                                ponuda["VrstaTereta"] = reader.GetString(4);
                            if (!reader.IsDBNull(5))
                                ponuda["TezinaTereta"] = reader.GetDecimal(5);
                            if (!reader.IsDBNull(6))
                                ponuda["DimenzijeTereta"] = reader.GetString(6);
                            if (!reader.IsDBNull(7))
                                ponuda["DatumPolaska"] = reader.GetDateTime(7);
                            if (!reader.IsDBNull(8))
                                ponuda["DatumIsporuke"] = reader.GetDateTime(8);
                            if (!reader.IsDBNull(9))
                                ponuda["TrajanjePuta"] = reader.GetInt32(9);
                            if (!reader.IsDBNull(10))
                                ponuda["DodatanOpis"] = reader.GetString(10);
                            if (!reader.IsDBNull(11))
                                ponuda["Cena"] = reader.GetDecimal(11);
                            if (!reader.IsDBNull(12))
                                ponuda["MestoPolaska"] = reader.GetString(12); // Pretpostavljamo da je varchar
                            if (!reader.IsDBNull(13))
                                ponuda["MestoIsporuke"] = reader.GetString(13); // Pretpostavljamo da je varchar

                            ponude.Add(ponuda);
                        }
                    }
                }
            }

            return ponude;
        }
    }
}

