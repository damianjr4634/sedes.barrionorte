using MySqlConnector;
using SedesBarrioNorte.Models;

namespace SedesBarrioNorte.Services;

public sealed class ConsultaComisionesService
{
    private readonly IConfiguration _configuration;

    public ConsultaComisionesService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<SedeOpcion>> ObtenerSedesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select tbl_fil.fil_denion, tbl_fil.fil_codigo
            from tbl_fil
            where tbl_fil.fil_codigo IN ('1','37','27','38','34','9','36','39','6','18','26','13','40')
            order by tbl_fil.fil_denion
            """;

        var resultado = new List<SedeOpcion>();

        await using var conn = new MySqlConnection(ObtenerConnectionString());
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            resultado.Add(new SedeOpcion
            {
                fil_denion = reader["fil_denion"]?.ToString() ?? string.Empty,
                fil_codigo = reader["fil_codigo"]?.ToString() ?? string.Empty
            });
        }

        return resultado;
    }

    public async Task<IReadOnlyList<AlumnoResultado>> ObtenerAlumnosAsync(
        IReadOnlyList<string> sedes,
        DateTime inicio,
        DateTime desde,
        DateTime hasta,
        CancellationToken cancellationToken = default)
    {
        var sql = ReemplazarParametros(QueryAlumnosTemplate, sedes, inicio, desde, hasta);
        var resultado = new List<AlumnoResultado>();

        await using var conn = new MySqlConnection(ObtenerConnectionString());
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            resultado.Add(new AlumnoResultado
            {
                codigo_alumno = LeerInt(reader, "codigo_alumno") ?? 0,
                filial = LeerTexto(reader, "filial"),
                alumno = LeerTexto(reader, "alumno"),
                mail = LeerTexto(reader, "mail"),
                estado = LeerTexto(reader, "estado"),
                carrera = LeerTexto(reader, "carrera"),
                importe_pagado = LeerDecimal(reader, "importe_pagado"),
                cantidad_total_cuotas = LeerInt(reader, "cantidad_total_cuotas"),
                PORCENTAJE = LeerDecimal(reader, "PORCENTAJE"),
                COMISION = LeerDecimal(reader, "COMISION")
            });
        }

        return resultado;
    }

    public async Task<IReadOnlyList<SedeResultado>> ObtenerSedesResumenAsync(
        IReadOnlyList<string> sedes,
        DateTime inicio,
        DateTime desde,
        DateTime hasta,
        CancellationToken cancellationToken = default)
    {
        var sql = ReemplazarParametros(QuerySedesTemplate, sedes, inicio, desde, hasta);
        var resultado = new List<SedeResultado>();

        await using var conn = new MySqlConnection(ObtenerConnectionString());
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            resultado.Add(new SedeResultado
            {
                filial = LeerTexto(reader, "filial"),
                importe_pagado = LeerDecimal(reader, "importe_pagado"),
                COMISION = LeerDecimal(reader, "COMISION")
            });
        }

        return resultado;
    }

    private string ObtenerConnectionString()
    {
        var connectionString = _configuration.GetConnectionString("Base1A");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("No se encontro la cadena de conexion 'ConnectionStrings:Base1A'.");
        }

        return connectionString;
    }

    private static string ReemplazarParametros(
        string queryTemplate,
        IReadOnlyList<string> sedes,
        DateTime inicio,
        DateTime desde,
        DateTime hasta)
    {
        if (sedes.Count == 0)
        {
            throw new InvalidOperationException("Debe seleccionar al menos una sede.");
        }

        var sedeSql = string.Join(",", sedes.Select(s => $"'{MySqlHelper.EscapeString(s)}'"));

        return queryTemplate
            .Replace("{$$ sede}", sedeSql, StringComparison.Ordinal)
            .Replace("{$$ inicio}", $"'{inicio:yyyy-MM-dd}'", StringComparison.Ordinal)
            .Replace("{$$ desde}", $"'{desde:yyyy-MM-dd}'", StringComparison.Ordinal)
            .Replace("{$$ hasta}", $"'{hasta:yyyy-MM-dd}'", StringComparison.Ordinal);
    }

    private static string LeerTexto(MySqlDataReader reader, string nombreCampo)
    {
        var valor = reader[nombreCampo];
        return valor is DBNull ? string.Empty : valor?.ToString() ?? string.Empty;
    }

    private static int? LeerInt(MySqlDataReader reader, string nombreCampo)
    {
        var valor = reader[nombreCampo];
        if (valor is DBNull)
        {
            return null;
        }

        return Convert.ToInt32(valor);
    }

    private static decimal? LeerDecimal(MySqlDataReader reader, string nombreCampo)
    {
        var valor = reader[nombreCampo];
        if (valor is DBNull)
        {
            return null;
        }

        return Convert.ToDecimal(valor);
    }

    private const string QueryAlumnosTemplate = """
        SELECT tbl_cte.cte_pers AS codigo_alumno,
              tbl_fil.fil_denion AS filial,
              substring(concat(tbl_als.als_aape,', ',tbl_als.als_anom),1,45) AS alumno,
              tbl_als.als_aemail AS mail,
              tbl_eso.eso_denion AS estado,
              substring(tbl_sen.sen_denion,1,45) AS carrera,
              SUM(tbl_cte.cte_importe)*(-1) AS importe_pagado,
             (SELECT COUNT(*)
              FROM tbl_cte tc
              WHERE  tc.cte_pers= tbl_cte.cte_pers
                     AND (tc.cte_tipcom = 4  OR (tbl_als.als_afil='13' AND tc.cte_tipcom = 3 AND tc.cte_ncuota=1))
                     and (tc.cte_xadel = '0' or tc.cte_xadel is NULL) AND tc.cte_codigo<= tbl_cte.cte_codigo
              ) AS cantidad_total_cuotas,
            case (SELECT COUNT(*)
              FROM tbl_cte tc
                 WHERE  tc.cte_pers= tbl_cte.cte_pers
                    AND (tc.cte_tipcom = 4  OR (tbl_als.als_afil='13' AND tc.cte_tipcom = 3 AND tc.cte_ncuota=1))
                           and (tc.cte_xadel = '0' or tc.cte_xadel is NULL) AND tc.cte_codigo<= tbl_cte.cte_codigo)
              when 1 then
            case tbl_als.als_afil
               when '13' then 0  when '9'  then 35  when '18'  then 35
               when '34'  then 35 when '37'  then 25  when '40'  then 20
               when '39'  then 20 when '27'  then 20  when '6'  then 25
               when '17'  then 25 when '1' then 30 when '38' then 20
               when '36' then 35  when '26'  then 25
            end
            when 2 then
            case tbl_als.als_afil
               when '13' then 0 when '9'  then 35  when '18'  then 35
               when '34'  then 35 when '37'  then 25  when '40'  then 20
               when '39'  then 20 when '27'  then 20  when '6'  then 25
               when '17'  then 25 when '1' then 30 when '38' then 20
               when '36'  then 35 when '26'  then 25
            end
            when 3 then
            case tbl_als.als_afil
               when '13' then 25 when '9'  then 25  when '18'  then 25
               when '34'  then 25 when '37'  then 25 when '40'  then 20
               when '39'  then 20 when '27'  then 20 when '6'  then 25
               when '17'  then 25 when '1' then 30 when '38' then 20
               when '36'  then 25 when '26'  then 25
            end
            when 4 then
            case tbl_als.als_afil
               when '13' then 25 when '9'  then 25 when '18'  then 25
               when '34'  then 25 when '37'  then 25 when '40'  then 20
               when '39'  then 20 when '27'  then 20 when '6'  then 25
               when '17'  then 25 when '1' then 30 when '38' then 20
               when '36'  then 25 when '26'  then 25
            end
            when 5 then
            case tbl_als.als_afil
               when '13' then 25 when '9'  then 25 when '18'  then 25
               when '34'  then 25  when '37'  then 25 when '40'  then 20
               when '39'  then 20 when '27'  then 20 when '6'  then 25
               when '17'  then 25 when '1' then 30 when '38' then 20
               when '36'  then 25 when '26'  then 25
            end
            when 6 then
            case tbl_als.als_afil
               when '13' then 25 when '9'  then 25  when '18'  then 25
               when '34'  then 25 when '37'  then 25 when '40'  then 20
               when '39'  then 20 when '27'  then 20  when '6'  then 25
               when '17'  then 25 when '1' then 30 when '38' then 20
               when '36'  then 25 when '26'  then 25
            end
            else
            case tbl_als.als_afil
               when '13' then 30  when '9'  then 30 when '18'  then 30
            when '34'  then 30 when '37'  then 30
               when '40'  then 20 when '39'  then 20 when '27'  then 20
               when '6'  then 30  when '17'  then 30 when '1' then 30
               when '38' then 20  when '36'  then 30 when '26'  then 30
            end
            END AS PORCENTAJE,

                round(SUM(tbl_cte.cte_importe)*(-1)*
                (case (SELECT COUNT(*)
                  FROM tbl_cte tc
                     WHERE  tc.cte_pers= tbl_cte.cte_pers
                        AND (tc.cte_tipcom = 4  OR (tbl_als.als_afil='13' AND tc.cte_tipcom = 3 AND tc.cte_ncuota=1))
                               and (tc.cte_xadel = '0' or tc.cte_xadel is NULL) AND tc.cte_codigo<= tbl_cte.cte_codigo)
                  when 1 then
                case tbl_als.als_afil
                   when '13' then 0  when '9'  then 35  when '18'  then 35
                   when '34'  then 35 when '37'  then 25  when '40'  then 20
                   when '39'  then 20 when '27'  then 20  when '6'  then 25
                   when '17'  then 25 when '1' then 30 when '38' then 20
                   when '36' then 35  when '26'  then 25
                end
                when 2 then
                case tbl_als.als_afil
                   when '13' then 0 when '9'  then 35  when '18'  then 35
                   when '34'  then 35 when '37'  then 25  when '40'  then 20
                   when '39'  then 20 when '27'  then 20  when '6'  then 25
                   when '17'  then 25 when '1' then 30 when '38' then 20
                   when '36'  then 35 when '26'  then 25
                end
                when 3 then
                case tbl_als.als_afil
                   when '13' then 25 when '9'  then 25  when '18'  then 25
                   when '34'  then 25 when '37'  then 25 when '40'  then 20
                   when '39'  then 20 when '27'  then 20 when '6'  then 25
                   when '17'  then 25 when '1' then 30 when '38' then 20
                   when '36'  then 25 when '26'  then 25
                end
                when 4 then
                case tbl_als.als_afil
                   when '13' then 25 when '9'  then 25 when '18'  then 25
                   when '34'  then 25 when '37'  then 25 when '40'  then 20
                   when '39'  then 20 when '27'  then 20 when '6'  then 25
                   when '17'  then 25 when '1' then 30 when '38' then 20
                   when '36'  then 25 when '26'  then 25
                end
                when 5 then
                case tbl_als.als_afil
                   when '13' then 25 when '9'  then 25 when '18'  then 25
                   when '34'  then 25  when '37'  then 25 when '40'  then 20
                   when '39'  then 20 when '27'  then 20 when '6'  then 25
                   when '17'  then 25 when '1' then 30 when '38' then 20
                   when '36'  then 25 when '26'  then 25
                end
                when 6 then
                case tbl_als.als_afil
                   when '13' then 25 when '9'  then 25  when '18'  then 25
                   when '34'  then 25 when '37'  then 25 when '40'  then 20
                   when '39'  then 20 when '27'  then 20  when '6'  then 25
                   when '17'  then 25 when '1' then 30 when '38' then 20
                   when '36'  then 25 when '26'  then 25
                end
                else
                case tbl_als.als_afil
                   when '13' then 30  when '9'  then 30 when '18'  then 30
                when '34'  then 30 when '37'  then 30
                   when '40'  then 20 when '39'  then 20 when '27'  then 20
                   when '6'  then 30  when '17'  then 30 when '1' then 30
                   when '38' then 20  when '36'  then 30 when '26'  then 30
                end
                END)/100.00,2) AS COMISION
        from tbl_cte
        left join tbl_als on tbl_cte.cte_pers = tbl_als.als_apers and tbl_cte.cte_leg = tbl_als.als_aleg
        left join tbl_cuo on tbl_cte.cte_cur = tbl_cuo.cuo_codigo
        left join tbl_sen on tbl_cuo.cuo_sec = tbl_sen.sen_codigo
        LEFT OUTER JOIN tbl_fil  ON tbl_fil.fil_codigo = tbl_als.als_afil
        LEFT OUTER JOIN tbl_eso ON tbl_eso.eso_codigo=tbl_als.als_aest
        WHERE tbl_als.als_afil IN ({$$ sede})
              AND (tbl_cte.cte_tipcom = 4  OR (tbl_als.als_afil='13' AND tbl_cte.cte_tipcom = 3 AND tbl_cte.cte_ncuota=1))
              and (tbl_cte.cte_xadel = '0' or tbl_cte.cte_xadel is NULL)
              and tbl_cte.cte_fecfac >= {$$ desde}
              and tbl_cte.cte_fecfac <= {$$ hasta}
              AND tbl_als.als_aest = '4'
        group by tbl_cte.cte_pers,tbl_fil.fil_denion, tbl_cte.cte_pers,tbl_cte.cte_leg,tbl_cte.cte_cuenta,tbl_cte.cte_aniocuota,tbl_cte.cte_ncuota,tbl_cte.cte_sucfac,tbl_cte.cte_recibo,tbl_cte.cte_cajero,tbl_cte.cte_lote
        UNION ALL
        SELECT
               ta.als_apers, tbl_fil.fil_denion,
               substring(concat(ta.als_aape,', ',ta.als_anom),1,45),
               ta.als_aemail,
               tbl_eso.eso_denion,
                substring(tbl_sen.sen_denion,1,45) AS sen_denion,
                null AS cte_importe,
                NULL AS cant_cuotas,
                NULL AS PORCENTAJE,
                NULL AS COMISION
        from tbl_als ta
        left join tbl_cuo on ta.als_cur = tbl_cuo.cuo_codigo
        left join tbl_sen on tbl_sen.sen_codigo = tbl_cuo.cuo_sec
        LEFT OUTER JOIN tbl_fil  ON tbl_fil.fil_codigo = ta.als_afil
        LEFT OUTER JOIN tbl_eso ON tbl_eso.eso_codigo=ta.als_aest
        WHERE ta.als_afil IN ({$$ sede})
              AND ta.als_aest = '4'
              AND NOT EXISTS(
                            SELECT 1
                            from tbl_cte tc
                            WHERE (TC.cte_tipcom = 4  OR (ta.als_afil='13' AND tc.cte_tipcom = 3 AND tc.cte_ncuota=1))
                                 and (tc.cte_xadel = '0' or tc.cte_xadel is NULL)
                                 and tc.cte_fecfac >= {$$ desde}
                                 and tc.cte_fecfac <= {$$ hasta}
                                 AND tc.cte_pers = ta.als_apers
                            )
        UNION ALL
        SELECT
               ta.als_apers, tbl_fil.fil_denion,
               substring(concat(ta.als_aape,', ',ta.als_anom),1,45),
               ta.als_aemail,
               tbl_eso.eso_denion,
               substring(tbl_sen.sen_denion,1,45) AS sen_denion,
               null AS cte_importe,
               NULL AS cant_cuotas,
                NULL AS PORCENTAJE,
                NULL AS COMISION
        from tbl_als ta
        left join tbl_cuo on ta.als_cur = tbl_cuo.cuo_codigo
        left join tbl_sen on tbl_sen.sen_codigo = tbl_cuo.cuo_sec
        LEFT OUTER JOIN tbl_fil  ON tbl_fil.fil_codigo = ta.als_afil
        LEFT OUTER JOIN tbl_eso ON tbl_eso.eso_codigo=ta.als_aest
        WHERE ta.als_afil IN ({$$ sede})
              AND ta.als_aest IN ('148','149','150','151','145','147')
              AND EXISTS(
                        SELECT 1
                        from tbl_cte tc
                        WHERE tc.cte_tipcom = 4
                             and (tc.cte_xadel = '0' or tc.cte_xadel is NULL)
                             and tc.cte_fecfac >= {$$ inicio}
                        and tc.cte_fecfac <= {$$ hasta}
                        AND tc.cte_pers = ta.als_apers
                        )
        ORDER BY 2, 3, 8
        """;

    private const string QuerySedesTemplate = """
        SELECT tbl_fil.fil_denion AS filial,
               SUM(tbl_cte.cte_importe*(-1)) AS importe_pagado,
                sum(round((tbl_cte.cte_importe)*(-1)*
                (case (SELECT COUNT(*)
                  FROM tbl_cte tc
                     WHERE  tc.cte_pers= tbl_cte.cte_pers
                        AND (tc.cte_tipcom = 4  OR (tbl_als.als_afil='13' AND tc.cte_tipcom = 3 AND tc.cte_ncuota=1))
                               and (tc.cte_xadel = '0' or tc.cte_xadel is NULL) AND tc.cte_codigo<= tbl_cte.cte_codigo)
                  when 1 then
                case tbl_als.als_afil
                   when '13' then 0  when '9'  then 35  when '18'  then 35
                   when '34'  then 35 when '37'  then 25  when '40'  then 20
                   when '39'  then 20 when '27'  then 20  when '6'  then 25
                   when '17'  then 25 when '1' then 30 when '38' then 20
                   when '36' then 35  when '26'  then 25
                end
                when 2 then
                case tbl_als.als_afil
                   when '13' then 0 when '9'  then 35  when '18'  then 35
                   when '34'  then 35 when '37'  then 25  when '40'  then 20
                   when '39'  then 20 when '27'  then 20  when '6'  then 25
                   when '17'  then 25 when '1' then 30 when '38' then 20
                   when '36'  then 35 when '26'  then 25
                end
                when 3 then
                case tbl_als.als_afil
                   when '13' then 25 when '9'  then 25  when '18'  then 25
                   when '34'  then 25 when '37'  then 25 when '40'  then 20
                   when '39'  then 20 when '27'  then 20 when '6'  then 25
                   when '17'  then 25 when '1' then 30 when '38' then 20
                   when '36'  then 25 when '26'  then 25
                end
                when 4 then
                case tbl_als.als_afil
                   when '13' then 25 when '9'  then 25 when '18'  then 25
                   when '34'  then 25 when '37'  then 25 when '40'  then 20
                   when '39'  then 20 when '27'  then 20 when '6'  then 25
                   when '17'  then 25 when '1' then 30 when '38' then 20
                   when '36'  then 25 when '26'  then 25
                end
                when 5 then
                case tbl_als.als_afil
                   when '13' then 25 when '9'  then 25 when '18'  then 25
                   when '34'  then 25  when '37'  then 25 when '40'  then 20
                   when '39'  then 20 when '27'  then 20 when '6'  then 25
                   when '17'  then 25 when '1' then 30 when '38' then 20
                   when '36'  then 25 when '26'  then 25
                end
                when 6 then
                case tbl_als.als_afil
                   when '13' then 25 when '9'  then 25  when '18'  then 25
                   when '34'  then 25 when '37'  then 25 when '40'  then 20
                   when '39'  then 20 when '27'  then 20  when '6'  then 25
                   when '17'  then 25 when '1' then 30 when '38' then 20
                   when '36'  then 25 when '26'  then 25
                end
                else
                case tbl_als.als_afil
                   when '13' then 30  when '9'  then 30 when '18'  then 30
                when '34'  then 30 when '37'  then 30
                   when '40'  then 20 when '39'  then 20 when '27'  then 20
                   when '6'  then 30  when '17'  then 30 when '1' then 30
                   when '38' then 20  when '36'  then 30 when '26'  then 30
                end
                END)/100.00,2)) AS COMISION
            from tbl_cte
            left join tbl_als on tbl_cte.cte_pers = tbl_als.als_apers and tbl_cte.cte_leg = tbl_als.als_aleg
            left join tbl_cuo on tbl_cte.cte_cur = tbl_cuo.cuo_codigo
            left join tbl_sen on tbl_cuo.cuo_sec = tbl_sen.sen_codigo
            LEFT OUTER JOIN tbl_fil  ON tbl_fil.fil_codigo = tbl_als.als_afil
            LEFT OUTER JOIN tbl_eso ON tbl_eso.eso_codigo=tbl_als.als_aest
            WHERE tbl_als.als_afil IN ({$$ sede})
            AND (tbl_cte.cte_tipcom = 4  OR (tbl_als.als_afil='13' AND tbl_cte.cte_tipcom = 3 AND tbl_cte.cte_ncuota=1))
                  and (tbl_cte.cte_xadel = '0' or tbl_cte.cte_xadel is NULL)
                  and tbl_cte.cte_fecfac >= {$$ desde}
            and tbl_cte.cte_fecfac <= {$$ hasta}
            AND tbl_als.als_aest = '4'
            group BY 1
        """;
}
