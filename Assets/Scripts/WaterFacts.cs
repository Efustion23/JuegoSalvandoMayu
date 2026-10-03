using UnityEngine;

// Datos breves sobre el ODS 6 y el cuidado del agua que se muestran en las pantallas del juego.
public static class WaterFacts
{
    private static readonly string[] facts =
    {
        "El ODS 6 busca garantizar agua limpia y saneamiento para todas las personas.",
        "La meta 6.3 pide reducir la contaminación del agua, evitar el vertimiento de residuos y reciclar más.",
        "La meta 6.6 pide proteger los ecosistemas del agua: bosques, humedales, ríos, acuíferos y lagos.",
        "Una bolsa de plástico puede tardar cientos de años en degradarse.",
        "El plástico no desaparece: se rompe en microplásticos que terminan en los peces y en el agua.",
        "Con la lluvia, la basura de las orillas llega al río: limpiar la ribera también cuida el agua.",
        "Las raíces de los árboles de la ribera sujetan el suelo y evitan que la tierra enturbie el río.",
        "Los bosques de queñua de los Andes protegen el suelo y retienen el agua que alimenta los ríos.",
        "Peces, ranas y aves son indicadores: si vuelven a un río, el agua está mejorando.",
        "Separar la basura en plástico, vidrio/metal y orgánico hace posible reciclarla.",
        "Lo que se tira río arriba lo reciben las comunidades río abajo: el agua nos une a todos.",
        "El río sostiene la siembra, el ganado y la vida de las comunidades de la sierra.",
    };

    public static string Random() => facts[UnityEngine.Random.Range(0, facts.Length)];

    // Parte el texto en lineas de a lo mas 'max' caracteres para que no se salga de los paneles.
    public static string Wrap(string text, int max)
    {
        var sb = new System.Text.StringBuilder();
        int len = 0;
        foreach (var w in text.Split(' '))
        {
            if (len > 0 && len + 1 + w.Length > max) { sb.Append('\n'); len = 0; }
            else if (len > 0) { sb.Append(' '); len++; }
            sb.Append(w); len += w.Length;
        }
        return sb.ToString();
    }
}
