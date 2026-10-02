using UnityEngine;

// Datos breves sobre el ODS 6 y el cuidado del agua que se muestran en las pantallas del juego.
public static class WaterFacts
{
    private static readonly string[] facts =
    {
        "El ODS 6 busca garantizar agua limpia y saneamiento para todas las personas.",
        "La meta 6.3 del ODS 6 pide reducir la contaminación del agua, eliminar el vertimiento de residuos y aumentar el reciclaje.",
        "La meta 6.6 pide proteger y restaurar los ecosistemas del agua: montañas, bosques, humedales, ríos, acuíferos y lagos.",
        "Una bolsa de plástico puede tardar cientos de años en degradarse.",
        "El plástico no desaparece: se rompe en microplásticos que terminan en los peces y en el agua.",
        "Con la lluvia, la basura de las orillas es arrastrada al río: limpiar la ribera también es cuidar el agua.",
        "Las raíces de los árboles de la ribera sujetan el suelo y evitan que la tierra enturbie el río.",
        "Los bosques de queñua de los Andes protegen el suelo y ayudan a retener el agua que alimenta los ríos.",
        "Peces, ranas y aves son indicadores: cuando vuelven a un río, es señal de que el agua está mejorando.",
        "Separar la basura en plástico, vidrio/metal y orgánico hace posible reciclarla.",
        "Lo que se tira río arriba lo reciben las comunidades río abajo: el agua nos une a todos.",
        "El río sostiene la siembra, el ganado y la vida de las comunidades de la sierra.",
    };

    public static string Random() => facts[UnityEngine.Random.Range(0, facts.Length)];
}
