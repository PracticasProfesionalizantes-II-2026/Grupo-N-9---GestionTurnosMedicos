using ChronoSaludWeb.TagHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ChronoSalud.Tests.Web;

/// <summary>
/// Pruebas de los tag helpers que unen cada campo con su mensaje de error
/// (paso 14).
/// </summary>
public class CamposAccesiblesTests
{
    // Una vista de mentira: un pedido y un ModelState vacíos.
    private static ViewContext Vista() => new()
    {
        HttpContext = new DefaultHttpContext(),
        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
    };

    private static ModelExpression Campo(string nombre)
        => new(nombre, new EmptyModelMetadataProvider().GetModelExplorerForType(typeof(string), null));

    // Aplica el tag helper a un campo con los atributos dados y devuelve cómo quedó.
    private static TagHelperOutput Procesar(ViewContext vista, string nombre, params (string Nombre, string Valor)[] atributos)
    {
        var lista = new TagHelperAttributeList();
        foreach (var (atributo, valor) in atributos)
            lista.Add(atributo, valor);

        var salida = new TagHelperOutput("input", lista,
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        var contexto = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "prueba");

        new CampoConErrorTagHelper { For = Campo(nombre), ViewContext = vista }.Process(contexto, salida);
        return salida;
    }

    private static string? Atributo(TagHelperOutput salida, string nombre)
        => salida.Attributes[nombre]?.Value?.ToString();

    [Fact]
    public void El_id_del_mensaje_lleva_prefijo_y_guiones_bajos()
    {
        Assert.Equal("error-Email", IdDeError.Para("Email"));
        Assert.Equal("error-Cuenta_Email", IdDeError.Para("Cuenta.Email"));
        Assert.Equal("error-Medicamentos_0__Dosis", IdDeError.Para("Medicamentos[0].Dosis"));
    }

    [Fact]
    public void Sin_error_el_campo_no_cambia()
    {
        var salida = Procesar(Vista(), "Email", ("type", "email"));

        Assert.Null(Atributo(salida, "aria-invalid"));
        Assert.Null(Atributo(salida, "aria-describedby"));
        Assert.Null(Atributo(salida, "autofocus"));
    }

    [Fact]
    public void Con_error_queda_invalido_y_unido_a_su_mensaje()
    {
        var vista = Vista();
        vista.ModelState.AddModelError("Email", "El email es obligatorio.");

        var salida = Procesar(vista, "Email", ("type", "email"));

        Assert.Equal("true", Atributo(salida, "aria-invalid"));
        Assert.Equal("error-Email", Atributo(salida, "aria-describedby"));
    }

    [Fact]
    public void Conserva_la_ayuda_que_ya_tenia()
    {
        var vista = Vista();
        vista.ModelState.AddModelError("Nueva", "Tiene que tener al menos 8 caracteres.");

        var salida = Procesar(vista, "Nueva", ("type", "password"), ("aria-describedby", "ayuda-nueva"));

        Assert.Equal("ayuda-nueva error-Nueva", Atributo(salida, "aria-describedby"));
    }

    [Fact]
    public void Un_campo_de_un_sub_modelo_tambien_se_une()
    {
        var vista = Vista();
        vista.ModelState.AddModelError("Cuenta.Nombre", "El nombre es obligatorio.");

        var salida = Procesar(vista, "Cuenta.Nombre", ("type", "text"));

        Assert.Equal("error-Cuenta_Nombre", Atributo(salida, "aria-describedby"));
    }

    [Fact]
    public void Solo_el_primer_campo_con_error_recibe_el_foco()
    {
        var vista = Vista();
        vista.ModelState.AddModelError("Nombre", "Falta el nombre.");
        vista.ModelState.AddModelError("Apellido", "Falta el apellido.");

        var primero = Procesar(vista, "Nombre", ("type", "text"));
        var segundo = Procesar(vista, "Apellido", ("type", "text"));

        Assert.Equal("autofocus", Atributo(primero, "autofocus"));
        Assert.Null(Atributo(segundo, "autofocus"));
        Assert.Equal("true", Atributo(segundo, "aria-invalid"));
    }

    [Fact]
    public void Con_errores_de_campo_se_saca_el_autofocus_escrito_en_la_vista()
    {
        // Como en el Registro: Nombre trae autofocus y el error es del Email.
        var vista = Vista();
        vista.ModelState.AddModelError("Email", "Ese email ya está registrado.");

        var nombre = Procesar(vista, "Nombre", ("type", "text"), ("autofocus", "autofocus"));
        var email = Procesar(vista, "Email", ("type", "email"));

        Assert.Null(Atributo(nombre, "autofocus"));
        Assert.Equal("autofocus", Atributo(email, "autofocus"));
    }

    [Fact]
    public void Con_un_error_general_el_autofocus_escrito_se_queda()
    {
        // Como en el Login: "Email o contraseña incorrectos" no es de un campo.
        var vista = Vista();
        vista.ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");

        var email = Procesar(vista, "Email", ("type", "email"), ("autofocus", "autofocus"));

        Assert.Equal("autofocus", Atributo(email, "autofocus"));
        Assert.Null(Atributo(email, "aria-invalid"));
    }

    [Fact]
    public void Un_campo_oculto_no_se_toca()
    {
        var vista = Vista();
        vista.ModelState.AddModelError("IdPaciente", "Elegí un paciente.");

        var salida = Procesar(vista, "IdPaciente", ("type", "hidden"));

        Assert.Null(Atributo(salida, "aria-invalid"));
        Assert.Null(Atributo(salida, "autofocus"));
    }

    [Fact]
    public void El_mensaje_de_error_recibe_su_id_y_respeta_uno_puesto_a_mano()
    {
        var contexto = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "prueba");
        TagHelperOutput Mensaje(params TagHelperAttribute[] atributos) => new("span", new TagHelperAttributeList(atributos),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        var sinId = Mensaje();
        new MensajeDeErrorTagHelper { For = Campo("Cuenta.Email"), ViewContext = Vista() }.Process(contexto, sinId);

        var conId = Mensaje(new TagHelperAttribute("id", "mio"));
        new MensajeDeErrorTagHelper { For = Campo("Email"), ViewContext = Vista() }.Process(contexto, conId);

        Assert.Equal("error-Cuenta_Email", Atributo(sinId, "id"));
        Assert.Equal("mio", Atributo(conId, "id"));
    }
}
