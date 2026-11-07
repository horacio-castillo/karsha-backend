using TarjetasPro.Domain.Entities;

namespace TarjetasPro.Infrastructure.Auth;

/// <summary>
/// Plantillas por profesión: cada tema define paleta, tipografía y tono (formal, cálido o festivo)
/// sobre un mismo diseño base. Agregar una profesión = agregar una línea a <see cref="All"/>.
/// </summary>
public static class ProfessionThemes
{
    public enum Tone { Formal, Warm, Festive }

    private record Theme(string Name, string Code, string Category, string Profession, string Description, Tone Tone,
        string Bg, string Surface, string Text, string Muted, string Primary, string Accent,
        string HeadFont, string BodyFont, string FontQuery);

    private static readonly Theme[] Themes =
    [
        // Salud
        new("Doctor", "doctor-formal", "Salud", "Médico / Doctor", "Sobrio y profesional: azul marino, tipografía con serifa.", Tone.Formal,
            "#eef2f7", "#ffffff", "#1b2a41", "#5c6b80", "#1f3b63", "#2f80a8", "Merriweather", "Source Sans 3", "Merriweather:wght@700&family=Source+Sans+3:wght@400;600"),
        new("Pediatra", "pediatra-dulce", "Salud", "Pediatra", "Rosa y turquesa suaves, con osito, corazones y estrellas.", Tone.Warm,
            "#fff1f4", "#ffffff", "#4a2c3a", "#8a6b7a", "#d6577e", "#4fb3a9", "Poppins", "Poppins", "Poppins:wght@400;600;700"),
        new("Dentista", "dentista-fresco", "Salud", "Dentista", "Limpio y fresco: blanco y turquesa.", Tone.Warm,
            "#effafa", "#ffffff", "#12363a", "#5a7f84", "#0f9aa6", "#7dd3d8", "Poppins", "Poppins", "Poppins:wght@400;600;700"),
        new("Psicólogo", "psicologo-calma", "Salud", "Psicólogo / Terapeuta", "Tonos salvia y cálidos que transmiten calma.", Tone.Warm,
            "#f3f5ef", "#fdfcf8", "#34423a", "#6f7d73", "#6b8f71", "#d9a77c", "Lora", "Nunito", "Lora:wght@600&family=Nunito:wght@400;700"),

        // Legal y contable
        new("Abogado", "abogado-clasico", "Legal y contable", "Abogado / Notario", "Formal y elegante: burdeos y dorado.", Tone.Formal,
            "#f6f1ea", "#fffdf9", "#2b1a1a", "#7a6a62", "#6d1f2c", "#b8913b", "Playfair Display", "Lato", "Playfair+Display:wght@700&family=Lato:wght@400;700"),
        new("Contador", "contador-serio", "Legal y contable", "Contador / Asesor fiscal", "Verde profundo y gris: confianza y orden.", Tone.Formal,
            "#eef1ee", "#ffffff", "#1d2b25", "#62716a", "#1e5a45", "#8a9a91", "Source Serif 4", "Inter", "Source+Serif+4:wght@700&family=Inter:wght@400;600"),

        // Belleza y bienestar
        new("Estilista", "estilista-glam", "Belleza y bienestar", "Estilista / Maquillista", "Rosa intenso y negro, moderno y glamuroso.", Tone.Warm,
            "#fff0f5", "#ffffff", "#2a1520", "#8a6574", "#d6336c", "#2a1520", "Montserrat", "Montserrat", "Montserrat:wght@400;600;800"),
        new("Barbero", "barbero-oscuro", "Belleza y bienestar", "Barbero", "Negro y cobre con letras fuertes.", Tone.Festive,
            "#141414", "#1f1f1f", "#f2ece4", "#a89c8e", "#c8773a", "#e8b27a", "Oswald", "Roboto", "Oswald:wght@500;700&family=Roboto:wght@400;500"),

        // Comida
        new("Restaurante", "restaurante-rustico", "Comida", "Chef / Restaurante", "Rojo tomate y crema, cálido y apetitoso.", Tone.Warm,
            "#fbf3e6", "#fffaf0", "#3b2417", "#8a6d58", "#b3261e", "#e0a030", "Playfair Display", "Lato", "Playfair+Display:wght@700&family=Lato:wght@400;700"),
        new("Pastelería", "pasteleria-dulce", "Comida", "Pastelero / Repostería", "Rosa pastel y chocolate, dulce y divertido.", Tone.Festive,
            "#fff1f4", "#ffffff", "#4a2c2a", "#9a7774", "#e8739a", "#8b5a3c", "Pacifico", "Quicksand", "Pacifico&family=Quicksand:wght@500;700"),

        // Educación
        new("Maestro", "maestro-claro", "Educación", "Maestro / Tutor", "Azul y amarillo, claro y cercano.", Tone.Warm,
            "#eef5ff", "#ffffff", "#1e2f4d", "#667a99", "#2f6fe4", "#f5b82e", "Nunito", "Nunito", "Nunito:wght@400;700;800"),

        // Oficios y servicios
        new("Mecánico", "mecanico-taller", "Oficios y servicios", "Mecánico / Taller", "Gris acero y rojo, aire de taller.", Tone.Festive,
            "#e9ebee", "#ffffff", "#1c2128", "#5f6773", "#d62828", "#2b303a", "Oswald", "Roboto", "Oswald:wght@500;700&family=Roboto:wght@400;500"),

        new("Veterinario", "veterinario-patitas", "Salud", "Veterinario", "Verde y naranja con huellitas: cercano con las mascotas.", Tone.Warm,
            "#f1f8ec", "#ffffff", "#25381d", "#6b8260", "#3f9a3a", "#f08a24", "Nunito", "Nunito", "Nunito:wght@400;700;800"),
        new("Nutriólogo", "nutriologo-fresco", "Salud", "Nutriólogo / Nutrióloga", "Verdes frescos y frutas, aire saludable.", Tone.Warm,
            "#f2faee", "#ffffff", "#243b1f", "#6a8663", "#5fae3c", "#ff7a45", "Poppins", "Poppins", "Poppins:wght@400;600;700"),
        new("Arquitecto", "arquitecto-plano", "Arquitectura e ingeniería", "Arquitecto / Ingeniero", "Gris grafito y azul plano, técnico y ordenado.", Tone.Formal,
            "#eef0f3", "#ffffff", "#1d2430", "#667085", "#274c77", "#e0a526", "Montserrat", "Inter", "Montserrat:wght@700&family=Inter:wght@400;600"),
        new("Albañil", "albanil-obra", "Oficios y servicios", "Albañil / Construcción", "Naranja obra y gris cemento, con ladrillos y cascos.", Tone.Festive,
            "#efece8", "#ffffff", "#2a2623", "#6f675f", "#e8590c", "#495057", "Oswald", "Roboto", "Oswald:wght@500;700&family=Roboto:wght@400;500"),
        new("Servicios generales", "servicios-generales", "Oficios y servicios", "Servicios generales / Mantenimiento", "Azul y gris, práctico para todo tipo de reparaciones.", Tone.Warm,
            "#eef2f6", "#ffffff", "#1d2733", "#62707e", "#2563eb", "#f59f00", "Poppins", "Poppins", "Poppins:wght@400;600;700"),
        new("Plomero", "plomero-agua", "Oficios y servicios", "Plomero", "Azul agua y amarillo, con gotas y llaves.", Tone.Festive,
            "#e4f4fb", "#ffffff", "#0f2f45", "#4f7a93", "#0b78c2", "#ffc21a", "Oswald", "Roboto", "Oswald:wght@500;700&family=Roboto:wght@400;500"),
        new("Electricista", "electricista-voltaje", "Oficios y servicios", "Electricista", "Negro y amarillo eléctrico, con rayos y focos.", Tone.Festive,
            "#fff8d6", "#ffffff", "#1a1a1a", "#6b6b55", "#e6a800", "#1a1a1a", "Oswald", "Roboto", "Oswald:wght@500;700&family=Roboto:wght@400;500"),
        new("Carpintero", "carpintero-madera", "Oficios y servicios", "Carpintero", "Tonos madera y crema, con herramientas.", Tone.Warm,
            "#f6ece0", "#fffaf3", "#3b2616", "#8a6c52", "#9a5b27", "#d9a05b", "Lora", "Nunito", "Lora:wght@600&family=Nunito:wght@400;700"),
        new("Jardinero", "jardinero-verde", "Oficios y servicios", "Jardinero / Paisajista", "Verde hoja y flores de colores.", Tone.Warm,
            "#eff8e8", "#ffffff", "#1f3a1c", "#648260", "#3c9d3a", "#f2b632", "Nunito", "Nunito", "Nunito:wght@400;700;800"),
        new("Fotógrafo", "fotografo-estudio", "Eventos y entretenimiento", "Fotógrafo / Video", "Oscuro y elegante, con cámaras y destellos.", Tone.Warm,
            "#1b1b1f", "#26262c", "#f4f1ec", "#a8a49c", "#e3b04b", "#e3b04b", "Montserrat", "Montserrat", "Montserrat:wght@400;600;800"),

        // Comercio
        new("Tienda", "tienda-colorida", "Comercio", "Tienda / Negocio local", "Alegre y directo, ideal para vender por WhatsApp.", Tone.Warm,
            "#fff7ec", "#ffffff", "#2d2a26", "#7d756b", "#f26b21", "#2a9d8f", "Poppins", "Poppins", "Poppins:wght@400;600;700"),

        // Eventos y entretenimiento
        new("Payaso", "payaso-circo", "Eventos y entretenimiento", "Payaso", "Colores primarios y letras gordas, como un circo.", Tone.Festive,
            "#fff4c2", "#ffffff", "#2b1f5c", "#6b5fa0", "#e63946", "#1d7fe0", "Fredoka", "Fredoka", "Fredoka:wght@400;600;700"),
        new("Animador infantil", "animador-fiesta", "Eventos y entretenimiento", "Animador infantil", "Confeti morado, naranja y turquesa para fiestas.", Tone.Festive,
            "#f3e8ff", "#ffffff", "#34195c", "#7d62a8", "#8e3dd6", "#ff8a1f", "Baloo 2", "Baloo 2", "Baloo+2:wght@500;700;800"),
        new("DJ", "dj-neon", "Eventos y entretenimiento", "DJ / Sonido para eventos", "Oscuro con neón, para la noche.", Tone.Festive,
            "#0b0b17", "#15152b", "#f4f4ff", "#9a9ac4", "#ff2e93", "#27e1ff", "Rajdhani", "Rajdhani", "Rajdhani:wght@500;600;700"),

        // ---- Catálogo ampliado: 40 profesiones y 40 oficios ----
        new("Cardiólogo", "cardiologo-latido", "Salud", "Cardiólogo", "Rojo corazón y gris, serio y cercano.", Tone.Formal,
            "#fbf1f1", "#ffffff", "#2b1b1d", "#7a6467", "#b3202f", "#e8707b", "Merriweather", "Source Sans 3", "Merriweather:wght@700&family=Source+Sans+3:wght@400;600"),
        new("Dermatólogo", "dermatologo-piel", "Salud", "Dermatólogo / Dermatóloga", "Tonos durazno y salvia, piel sana.", Tone.Warm,
            "#fdf4ee", "#ffffff", "#3b2a22", "#8a7468", "#c9805a", "#9bb59a", "Lora", "Nunito", "Lora:wght@600&family=Nunito:wght@400;700"),
        new("Ginecólogo", "ginecologa-rosa", "Salud", "Ginecólogo / Ginecóloga", "Rosa suave y malva, delicado y confiable.", Tone.Warm,
            "#fff1f6", "#ffffff", "#3d2230", "#8f6b7d", "#c2477a", "#b79ad6", "Poppins", "Poppins", "Poppins:wght@400;600;700"),
        new("Optometrista", "optica-claro", "Salud", "Oftalmólogo / Optometrista", "Azul claro y gris, nítido y limpio.", Tone.Formal,
            "#eef5fb", "#ffffff", "#14263a", "#5d7287", "#1f6fb2", "#74b3e0", "Poppins", "Poppins", "Poppins:wght@400;600;700"),
        new("Traumatólogo", "traumatologo-fuerza", "Salud", "Traumatólogo / Ortopedista", "Azul petróleo y naranja, firme y deportivo.", Tone.Formal,
            "#edf3f4", "#ffffff", "#172a30", "#5b7077", "#176b7a", "#f08a3c", "Oswald", "Roboto", "Oswald:wght@500;700&family=Roboto:wght@400;500"),
        new("Fisioterapeuta", "fisioterapia-movimiento", "Salud", "Fisioterapeuta", "Verde menta y azul, movimiento y bienestar.", Tone.Warm,
            "#eefaf5", "#ffffff", "#14352c", "#5d8076", "#1f9c7a", "#5ac0e0", "Nunito", "Nunito", "Nunito:wght@400;700;800"),
        new("Enfermero", "enfermeria-cuidado", "Salud", "Enfermero / Enfermera", "Turquesa y blanco, cuidado cálido.", Tone.Warm,
            "#eefafa", "#ffffff", "#123638", "#5b8082", "#15909a", "#f29aae", "Nunito", "Nunito", "Nunito:wght@400;700;800"),
        new("Psiquiatra", "psiquiatra-serenidad", "Salud", "Psiquiatra", "Azul noche y lavanda, sereno y profesional.", Tone.Formal,
            "#f1f0f9", "#ffffff", "#1f1d3a", "#6e6c8c", "#3f3d8f", "#a89be0", "Lora", "Nunito", "Lora:wght@600&family=Nunito:wght@400;700"),
        new("Farmacéutico", "farmacia-verde", "Salud", "Farmacéutico / Farmacia", "Verde cruz y blanco, claro y confiable.", Tone.Warm,
            "#eef8f1", "#ffffff", "#14301f", "#5d7f69", "#1e9a55", "#7ad3a0", "Poppins", "Poppins", "Poppins:wght@400;600;700"),
        new("Cirujano plástico", "cirujano-estetica", "Salud", "Cirujano plástico", "Champagne y negro, elegante y discreto.", Tone.Formal,
            "#f7f2ec", "#ffffff", "#231d17", "#7d7266", "#8a6d3b", "#c8a96a", "Playfair Display", "Lato", "Playfair+Display:wght@700&family=Lato:wght@400;700"),
        new("Neurólogo", "neurologo-mente", "Salud", "Neurólogo", "Violeta y azul, ciencia y claridad.", Tone.Formal,
            "#f0eefa", "#ffffff", "#1c1838", "#6a6590", "#5b46c4", "#8ab6f0", "Source Serif 4", "Inter", "Source+Serif+4:wght@700&family=Inter:wght@400;600"),
        new("Urólogo", "urologo-azul", "Salud", "Urólogo", "Azul sobrio y gris, discreto y profesional.", Tone.Formal,
            "#eff3f8", "#ffffff", "#15233a", "#5f6f86", "#25508f", "#6f94c7", "Merriweather", "Source Sans 3", "Merriweather:wght@700&family=Source+Sans+3:wght@400;600"),
        new("Otorrino", "otorrino-sonido", "Salud", "Otorrinolaringólogo", "Turquesa y coral, sonido y respiración.", Tone.Warm,
            "#eef7f8", "#ffffff", "#153237", "#5c7d82", "#1a8a99", "#f08a7a", "Poppins", "Poppins", "Poppins:wght@400;600;700"),
        new("Endocrinólogo", "endocrinologo-balance", "Salud", "Endocrinólogo", "Verde azulado y ámbar, equilibrio.", Tone.Formal,
            "#eef6f3", "#ffffff", "#15302a", "#5c7d74", "#1b7f6b", "#e0a43c", "Lora", "Nunito", "Lora:wght@600&family=Nunito:wght@400;700"),
        new("Laboratorio clínico", "laboratorio-clinico", "Salud", "Laboratorio clínico / Biólogo", "Azul laboratorio y verde, preciso y limpio.", Tone.Formal,
            "#eef4fa", "#ffffff", "#14283a", "#5b7186", "#1d6fa5", "#5cc2a0", "Source Serif 4", "Inter", "Source+Serif+4:wght@700&family=Inter:wght@400;600"),
        new("Asesor financiero", "asesor-financiero", "Legal y contable", "Asesor financiero", "Verde billete y marino, confianza y crecimiento.", Tone.Formal,
            "#eef3f1", "#ffffff", "#14261f", "#5c726a", "#17624a", "#d0a93c", "Source Serif 4", "Inter", "Source+Serif+4:wght@700&family=Inter:wght@400;600"),
        new("Agente de seguros", "seguros-proteccion", "Legal y contable", "Agente de seguros", "Azul protección y celeste, seguro y cercano.", Tone.Warm,
            "#eef4fb", "#ffffff", "#14263d", "#5b7088", "#1b5fa8", "#78b7ec", "Poppins", "Poppins", "Poppins:wght@400;600;700"),
        new("Perito valuador", "perito-valuador", "Legal y contable", "Perito / Valuador", "Gris carbón y cobre, técnico y exacto.", Tone.Formal,
            "#f1f0ee", "#ffffff", "#221f1c", "#736d66", "#5b4a3a", "#b87a45", "Merriweather", "Source Sans 3", "Merriweather:wght@700&family=Source+Sans+3:wght@400;600"),
        new("Profesor de idiomas", "profesor-idiomas", "Educación", "Profesor de idiomas", "Azul y rojo vivo, comunicación sin fronteras.", Tone.Warm,
            "#eef3fb", "#ffffff", "#16233a", "#5e6f8a", "#1f4fc0", "#e8504b", "Poppins", "Poppins", "Poppins:wght@400;600;700"),
        new("Coach", "coach-impulso", "Educación", "Coach / Mentor", "Naranja y marino, motivación y energía.", Tone.Warm,
            "#fff3ea", "#ffffff", "#2a1c14", "#8a6e5c", "#e0662b", "#2c4a7a", "Montserrat", "Montserrat", "Montserrat:wght@400;600;800"),
        new("Maestro de música", "maestro-musica", "Educación", "Maestro de música", "Morado y dorado, notas y ritmo.", Tone.Festive,
            "#f4effb", "#ffffff", "#251a3a", "#7a6a94", "#6a3fc0", "#f0b429", "Poppins", "Poppins", "Poppins:wght@400;600;700"),
        new("Maestra de preescolar", "maestra-preescolar", "Educación", "Maestra de preescolar", "Colores primarios suaves, alegre y tierno.", Tone.Festive,
            "#fff8e8", "#ffffff", "#2b2a3a", "#7d7a90", "#ef6c3d", "#3ba7e0", "Nunito", "Nunito", "Nunito:wght@400;700;800"),
        new("Psicopedagogo", "psicopedagogia-aprende", "Educación", "Psicopedagogo", "Verde lima y azul, aprendizaje y calma.", Tone.Warm,
            "#f1f8ec", "#ffffff", "#1f3320", "#67806a", "#4c9a2a", "#4aa3d8", "Nunito", "Nunito", "Nunito:wght@400;700;800"),
        new("Ingeniero civil", "ingeniero-civil", "Arquitectura e ingeniería", "Ingeniero civil", "Amarillo obra y grafito, sólido y técnico.", Tone.Formal,
            "#f2f1ec", "#ffffff", "#23221c", "#77746a", "#3d3b36", "#e8b020", "Oswald", "Roboto", "Oswald:wght@500;700&family=Roboto:wght@400;500"),
        new("Desarrollador", "desarrollador-codigo", "Arquitectura e ingeniería", "Desarrollador de software", "Azul eléctrico y menta, código limpio.", Tone.Formal,
            "#eef1f9", "#ffffff", "#151a2e", "#5f678a", "#2f4ee0", "#3cd0b0", "Source Serif 4", "Inter", "Source+Serif+4:wght@700&family=Inter:wght@400;600"),
        new("Diseñador de interiores", "interiorismo-calido", "Arquitectura e ingeniería", "Diseñador de interiores", "Arena y verde olivo, cálido y refinado.", Tone.Warm,
            "#f6f1ea", "#ffffff", "#2b241c", "#857868", "#7a8a4a", "#c9a27a", "Playfair Display", "Lato", "Playfair+Display:wght@700&family=Lato:wght@400;700"),
        new("Diseñador gráfico", "diseno-grafico", "Arquitectura e ingeniería", "Diseñador gráfico", "Magenta y cian, creativo y atrevido.", Tone.Festive,
            "#fdf0f7", "#ffffff", "#2a1530", "#8a6a90", "#d6249f", "#19b5d8", "Montserrat", "Montserrat", "Montserrat:wght@400;600;800"),
        new("Topógrafo", "topografo-terreno", "Arquitectura e ingeniería", "Topógrafo", "Verde tierra y naranja, preciso en campo.", Tone.Formal,
            "#f0f2ea", "#ffffff", "#1f2a1a", "#6b7a62", "#4e6b2a", "#e08a2c", "Oswald", "Roboto", "Oswald:wght@500;700&family=Roboto:wght@400;500"),
        new("Ingeniero industrial", "ingeniero-industrial", "Arquitectura e ingeniería", "Ingeniero industrial", "Azul acero y naranja, procesos y eficiencia.", Tone.Formal,
            "#eef1f5", "#ffffff", "#16212e", "#5f6d7e", "#2a4d7a", "#e8742c", "Source Serif 4", "Inter", "Source+Serif+4:wght@700&family=Inter:wght@400;600"),
        new("Ingeniero mecánico", "ingeniero-mecanico", "Arquitectura e ingeniería", "Ingeniero mecánico", "Gris metal y rojo, potencia y precisión.", Tone.Formal,
            "#eeeff1", "#ffffff", "#1c1f24", "#686d77", "#4a525f", "#d33f3f", "Oswald", "Roboto", "Oswald:wght@500;700&family=Roboto:wght@400;500"),
        new("Soldador", "herrero-fuego", "Oficios y servicios", "Soldador / Herrero", "Negro y naranja fuego, fuerte y artesanal.", Tone.Festive,
            "#f1ece8", "#ffffff", "#241e1a", "#7a6c62", "#d9480f", "#3b3835", "Oswald", "Roboto", "Oswald:wght@500;700&family=Roboto:wght@400;500"),
        new("Pintor", "pintor-color", "Oficios y servicios", "Pintor", "Colores vivos sobre blanco, brocha y rodillo.", Tone.Festive,
            "#fff7ec", "#ffffff", "#2a2218", "#8a7a66", "#e8442e", "#2d9cdb", "Poppins", "Poppins", "Poppins:wght@400;600;700"),
        new("Cerrajero", "cerrajero-llave", "Oficios y servicios", "Cerrajero", "Dorado y azul oscuro, seguridad las 24 horas.", Tone.Formal,
            "#eef0f4", "#ffffff", "#151b2b", "#606880", "#1d2f5a", "#e3b13a", "Oswald", "Roboto", "Oswald:wght@500;700&family=Roboto:wght@400;500"),
        new("Aire acondicionado", "aire-acondicionado", "Oficios y servicios", "Aire acondicionado / Refrigeración", "Azul hielo y blanco, fresco y técnico.", Tone.Warm,
            "#eaf6fc", "#ffffff", "#0f2f42", "#5a7f94", "#1a8ac9", "#7fd0f0", "Poppins", "Poppins", "Poppins:wght@400;600;700"),
        new("Técnico en computadoras", "tecnico-computadoras", "Oficios y servicios", "Técnico en computadoras", "Azul digital y verde, soporte y reparación.", Tone.Warm,
            "#eef2fb", "#ffffff", "#141b33", "#5f698c", "#2b55d6", "#25c28a", "Source Serif 4", "Inter", "Source+Serif+4:wght@700&family=Inter:wght@400;600"),
        new("Reparación de celulares", "reparacion-celulares", "Oficios y servicios", "Reparación de celulares", "Violeta y cian, pantalla nueva en minutos.", Tone.Festive,
            "#f2eefc", "#ffffff", "#1c1632", "#6f6890", "#6c3bd6", "#20c0e0", "Poppins", "Poppins", "Poppins:wght@400;600;700"),
        new("Mudanzas", "mudanzas-fletes", "Oficios y servicios", "Mudanzas y fletes", "Amarillo y azul, cargamos todo contigo.", Tone.Festive,
            "#fffbe8", "#ffffff", "#1d2433", "#6c7488", "#1f4fb5", "#f5b800", "Oswald", "Roboto", "Oswald:wght@500;700&family=Roboto:wght@400;500"),
        new("Limpieza", "limpieza-brillo", "Oficios y servicios", "Limpieza y mantenimiento", "Turquesa y lima, brillo y frescura.", Tone.Warm,
            "#edf9f8", "#ffffff", "#11332f", "#5a8480", "#14a5a0", "#a6d933", "Nunito", "Nunito", "Nunito:wght@400;700;800"),
        new("Fumigador", "fumigador-control", "Oficios y servicios", "Fumigador / Control de plagas", "Verde y amarillo alerta, sin plagas.", Tone.Festive,
            "#f1f6e8", "#ffffff", "#1c2a14", "#6a7d58", "#3d8b1f", "#f2c200", "Oswald", "Roboto", "Oswald:wght@500;700&family=Roboto:wght@400;500"),
        new("Costurera", "costurera-aguja", "Oficios y servicios", "Costurera / Sastre", "Rosa viejo y crema, hecho a tu medida.", Tone.Warm,
            "#fbf3ef", "#ffffff", "#33211f", "#8c7370", "#b05a6a", "#d8b48a", "Lora", "Nunito", "Lora:wght@600&family=Nunito:wght@400;700"),
        new("Manicurista", "manicurista-unas", "Belleza y bienestar", "Manicurista / Uñas", "Rosa chicle y dorado, uñas de revista.", Tone.Festive,
            "#fff0f6", "#ffffff", "#3a1a2b", "#94677f", "#e0409a", "#f0c04a", "Poppins", "Poppins", "Poppins:wght@400;600;700"),
        new("Spa y masajes", "spa-masajes", "Belleza y bienestar", "Masajista / Spa", "Verde té y piedra, relajación total.", Tone.Warm,
            "#f1f4ee", "#ffffff", "#26301f", "#77826c", "#6f8f5a", "#c9b48a", "Lora", "Nunito", "Lora:wght@600&family=Nunito:wght@400;700"),
        new("Entrenador personal", "entrenador-fuerza", "Belleza y bienestar", "Entrenador personal / Gym", "Verde oscuro y neón, disciplina y resultados.", Tone.Festive,
            "#f0f3ea", "#ffffff", "#14190f", "#647058", "#1f2a14", "#8ee03a", "Oswald", "Roboto", "Oswald:wght@500;700&family=Roboto:wght@400;500"),
        new("Yoga", "yoga-armonia", "Belleza y bienestar", "Instructor de yoga / Pilates", "Lila y durazno, calma y respiración.", Tone.Warm,
            "#f6f1fb", "#ffffff", "#2c2238", "#85799a", "#8a63c9", "#f2b49a", "Nunito", "Nunito", "Nunito:wght@400;700;800"),
        new("Cosmetóloga", "cosmetologia-rostro", "Belleza y bienestar", "Cosmetóloga / Faciales", "Coral y crema, piel luminosa.", Tone.Warm,
            "#fff3f0", "#ffffff", "#3a2420", "#94736c", "#d9695a", "#f0c9a0", "Playfair Display", "Lato", "Playfair+Display:wght@700&family=Lato:wght@400;700"),
        new("Taquería", "taqueria-sabor", "Comida", "Taquería", "Rojo chile y amarillo maíz, ¡a taquear!", Tone.Festive,
            "#fff6df", "#ffffff", "#301a10", "#8a6a50", "#d4321c", "#f2b600", "Oswald", "Roboto", "Oswald:wght@500;700&family=Roboto:wght@400;500"),
        new("Panadería", "panaderia-horno", "Comida", "Panadería", "Trigo y chocolate, recién horneado.", Tone.Warm,
            "#fbf3e4", "#ffffff", "#34231a", "#8f7762", "#a8641f", "#e8c070", "Lora", "Nunito", "Lora:wght@600&family=Nunito:wght@400;700"),
        new("Cafetería", "cafeteria-grano", "Comida", "Cafetería", "Café y crema, aroma de la mañana.", Tone.Warm,
            "#f5eee6", "#ffffff", "#2b1d14", "#85705e", "#6b3f22", "#d9a066", "Playfair Display", "Lato", "Playfair+Display:wght@700&family=Lato:wght@400;700"),
        new("Banquetes", "banquetes-fiesta", "Comida", "Banquetes / Cocina económica", "Rojo vino y dorado, comida para todos.", Tone.Warm,
            "#fbf1ea", "#ffffff", "#321d17", "#8c7066", "#a3342b", "#e0a53a", "Lora", "Nunito", "Lora:wght@600&family=Nunito:wght@400;700"),
        new("Inmobiliaria", "inmobiliaria-llave", "Comercio", "Agente inmobiliario", "Azul marino y cobre, tu próxima casa.", Tone.Formal,
            "#eef1f5", "#ffffff", "#151f33", "#5f6b82", "#1f3a68", "#c47a3d", "Source Serif 4", "Inter", "Source+Serif+4:wght@700&family=Inter:wght@400;600"),
        new("Marketing digital", "marketing-digital", "Comercio", "Marketing digital / Community manager", "Magenta y naranja, redes que venden.", Tone.Festive,
            "#fff1f0", "#ffffff", "#2d1517", "#8f6a6c", "#e6246a", "#ff9a1f", "Montserrat", "Montserrat", "Montserrat:wght@400;600;800"),
        new("Florería", "floreria-petalos", "Comercio", "Florería", "Rosa, verde y lila, flores para cada ocasión.", Tone.Warm,
            "#fdf1f4", "#ffffff", "#35212a", "#906f7c", "#d6457e", "#7bbf7a", "Lora", "Nunito", "Lora:wght@600&family=Nunito:wght@400;700"),
        new("Boutique", "boutique-moda", "Comercio", "Boutique de ropa", "Negro y rosa palo, tendencia y estilo.", Tone.Formal,
            "#f8f1f1", "#ffffff", "#231a1d", "#7d6b70", "#3a2d33", "#d9a1a8", "Playfair Display", "Lato", "Playfair+Display:wght@700&family=Lato:wght@400;700"),
        new("Mariachi", "mariachi-fiesta", "Eventos y entretenimiento", "Mariachi / Músico", "Verde bandera, rojo y dorado, serenatas.", Tone.Festive,
            "#fbf4e4", "#ffffff", "#261a10", "#86705a", "#1f6b3a", "#c8321f", "Playfair Display", "Lato", "Playfair+Display:wght@700&family=Lato:wght@400;700"),
        new("Organizador de eventos", "organizador-eventos", "Eventos y entretenimiento", "Organizador de eventos", "Turquesa y dorado, tu fiesta sin estrés.", Tone.Festive,
            "#eefaf8", "#ffffff", "#12302d", "#5b807c", "#1aa59a", "#f0b429", "Poppins", "Poppins", "Poppins:wght@400;600;700"),
    ];

    /// <summary>Emojis por profesión u oficio: paleta del editor de iconos y viñetas de la lista de servicios.</summary>
    private static readonly Dictionary<string, string[]> Icons = new()
    {
        ["doctor-formal"] = ["🩺", "💊", "❤️", "⚕️", "🏥", "💉", "🩹", "🌡️"],
        ["pediatra-dulce"] = ["🧸", "🍼", "👶", "❤️", "⭐", "🩺", "🌈", "💉"],
        ["dentista-fresco"] = ["🦷", "🪥", "✨", "😁", "💙", "🫧", "🩵", "⭐"],
        ["psicologo-calma"] = ["🧠", "🌿", "💚", "🕊️", "☀️", "🍃", "💭", "🌸"],
        ["veterinario-patitas"] = ["🐶", "🐱", "🐾", "🦴", "💚", "🐰", "🐹", "🩺"],
        ["nutriologo-fresco"] = ["🥑", "🍎", "🥦", "🥕", "🍊", "🥗", "🍋", "💚"],
        ["abogado-clasico"] = ["⚖️", "📜", "🏛️", "🖋️", "📘", "🔏", "📑", "⭐"],
        ["contador-serio"] = ["🧮", "📊", "💵", "📈", "🧾", "💼", "🪙", "📋"],
        ["arquitecto-plano"] = ["📐", "🏗️", "🏠", "📏", "✏️", "🧱", "🏢", "🗺️"],
        ["maestro-claro"] = ["📚", "✏️", "🍎", "🎓", "🔬", "🖍️", "🧮", "⭐"],
        ["estilista-glam"] = ["✂️", "💄", "💅", "💇", "🪞", "👠", "✨", "🌸"],
        ["barbero-oscuro"] = ["💈", "✂️", "🪒", "🧔", "💇‍♂️", "🧴", "⭐", "🔥"],
        ["restaurante-rustico"] = ["🍴", "🍅", "🌶️", "🍝", "🥖", "🍷", "🧄", "🌿"],
        ["pasteleria-dulce"] = ["🧁", "🎂", "🍰", "🍓", "🍩", "🍪", "🍫", "💖"],
        ["mecanico-taller"] = ["🔧", "⚙️", "🚗", "🛞", "🔩", "🛠️", "⛽", "🔋"],
        ["albanil-obra"] = ["🧱", "🏗️", "⛑️", "🔨", "📏", "🏠", "🪜", "🛠️"],
        ["servicios-generales"] = ["🛠️", "🔧", "🪛", "🧰", "🔩", "🏠", "🧹", "⭐"],
        ["plomero-agua"] = ["🔧", "🚰", "💧", "🚿", "🛁", "🪠", "🔩", "💦"],
        ["electricista-voltaje"] = ["⚡", "💡", "🔌", "🔋", "🛠️", "🔦", "⚙️", "🔆"],
        ["carpintero-madera"] = ["🪚", "🔨", "🪵", "📏", "🛠️", "🚪", "🪑", "🔩"],
        ["jardinero-verde"] = ["🌱", "🌳", "🌻", "🌷", "🪴", "🐝", "🍃", "💧"],
        ["tienda-colorida"] = ["🛍️", "🏷️", "🎁", "🛒", "💳", "⭐", "📦", "💛"],
        ["payaso-circo"] = ["🤡", "🎈", "🎪", "🎉", "🎩", "🤹", "🃏", "⭐"],
        ["animador-fiesta"] = ["🎈", "🎉", "🎂", "🎁", "🪅", "🧸", "🎊", "🦄"],
        ["dj-neon"] = ["🎧", "🎵", "🎶", "🔊", "🎤", "🪩", "💿", "⚡"],
        ["fotografo-estudio"] = ["📷", "🎞️", "💡", "🖼️", "✨", "📸", "🎬", "⭐"],
        ["cardiologo-latido"] = ["❤️", "🫀", "🩺", "💓", "🏥", "📈", "⚕️", "💊"],
        ["dermatologo-piel"] = ["✨", "🧴", "🌿", "🌸", "🩺", "💆", "☀️", "💧"],
        ["ginecologa-rosa"] = ["🌸", "🤰", "💗", "🩺", "👶", "🌷", "⚕️", "✨"],
        ["optica-claro"] = ["👓", "👁️", "🔍", "✨", "🕶️", "🩺", "💙", "⭐"],
        ["traumatologo-fuerza"] = ["🦴", "🦵", "🏃", "🩼", "🩺", "💪", "⚕️", "🏥"],
        ["fisioterapia-movimiento"] = ["💪", "🏃", "🧘", "🩺", "🦴", "💆", "✨", "💚"],
        ["enfermeria-cuidado"] = ["💉", "🩺", "❤️", "🏥", "🩹", "😷", "💊", "⭐"],
        ["psiquiatra-serenidad"] = ["🧠", "💜", "🕊️", "🌙", "🩺", "💭", "⚕️", "🌿"],
        ["farmacia-verde"] = ["💊", "➕", "🧪", "🩺", "🏪", "💚", "⚕️", "🧴"],
        ["cirujano-estetica"] = ["✨", "🩺", "⭐", "💎", "🌸", "🏥", "⚕️", "🪞"],
        ["neurologo-mente"] = ["🧠", "⚡", "🔬", "🩺", "💜", "🧬", "⚕️", "📋"],
        ["urologo-azul"] = ["🩺", "💧", "⚕️", "🏥", "💙", "🔬", "📋", "⭐"],
        ["otorrino-sonido"] = ["👂", "👃", "🩺", "🎧", "💙", "🏥", "⚕️", "✨"],
        ["endocrinologo-balance"] = ["🧬", "🩺", "⚖️", "💚", "🍎", "📈", "⚕️", "🔬"],
        ["laboratorio-clinico"] = ["🔬", "🧪", "🧬", "🩸", "🩺", "📋", "🏥", "⭐"],
        ["asesor-financiero"] = ["💼", "📈", "💵", "🏦", "🧾", "📊", "🤝", "⭐"],
        ["seguros-proteccion"] = ["🛡️", "🤝", "🚗", "🏠", "❤️", "📋", "☂️", "⭐"],
        ["perito-valuador"] = ["📏", "🏠", "📋", "🔍", "🧾", "🏗️", "⚖️", "⭐"],
        ["profesor-idiomas"] = ["🌎", "🗣️", "📚", "✏️", "🎓", "💬", "✈️", "⭐"],
        ["coach-impulso"] = ["🚀", "🎯", "💡", "⭐", "🏆", "💪", "🤝", "📈"],
        ["maestro-musica"] = ["🎵", "🎹", "🎸", "🎻", "🎤", "🎶", "🥁", "⭐"],
        ["maestra-preescolar"] = ["🧸", "🎨", "✏️", "🔤", "🌈", "📚", "🎈", "⭐"],
        ["psicopedagogia-aprende"] = ["🧩", "📚", "🧠", "✏️", "🌱", "💡", "🎓", "⭐"],
        ["ingeniero-civil"] = ["🏗️", "📐", "🌉", "🏢", "🧱", "⛑️", "📏", "⭐"],
        ["desarrollador-codigo"] = ["💻", "⌨️", "🚀", "🧩", "☁️", "🔧", "📱", "⭐"],
        ["interiorismo-calido"] = ["🛋️", "🪑", "🏠", "🪴", "💡", "🎨", "📐", "⭐"],
        ["diseno-grafico"] = ["🎨", "✏️", "🖌️", "💡", "🖼️", "📐", "✨", "⭐"],
        ["topografo-terreno"] = ["📍", "📐", "🗺️", "🧭", "📏", "🏞️", "🔭", "⭐"],
        ["ingeniero-industrial"] = ["⚙️", "🏭", "📈", "🔧", "📊", "🛠️", "🚚", "⭐"],
        ["ingeniero-mecanico"] = ["⚙️", "🔩", "🔧", "🏎️", "🛠️", "📐", "🏭", "⭐"],
        ["herrero-fuego"] = ["🔥", "🛠️", "⚙️", "🔩", "🏗️", "🔨", "⭐", "🚪"],
        ["pintor-color"] = ["🎨", "🖌️", "🏠", "🪜", "🧰", "✨", "🌈", "⭐"],
        ["cerrajero-llave"] = ["🔑", "🔒", "🚪", "🛠️", "🚗", "⏰", "🛡️", "⭐"],
        ["aire-acondicionado"] = ["❄️", "🌬️", "🛠️", "🔧", "🧊", "⚡", "🏠", "⭐"],
        ["tecnico-computadoras"] = ["💻", "🖥️", "🔧", "🛠️", "📶", "💾", "🔌", "⭐"],
        ["reparacion-celulares"] = ["📱", "🔧", "🔋", "🛠️", "⚡", "🧰", "✨", "⭐"],
        ["mudanzas-fletes"] = ["🚚", "📦", "🏠", "💪", "🧰", "📍", "⏰", "⭐"],
        ["limpieza-brillo"] = ["🧹", "🧽", "✨", "🧴", "🪣", "🏠", "💧", "⭐"],
        ["fumigador-control"] = ["🐜", "🪲", "🐀", "🧪", "🏠", "🛡️", "✅", "⭐"],
        ["costurera-aguja"] = ["🧵", "🪡", "✂️", "👗", "👔", "📏", "🧶", "⭐"],
        ["manicurista-unas"] = ["💅", "✨", "💖", "🌸", "🪞", "💎", "⭐", "🎀"],
        ["spa-masajes"] = ["💆", "🧖", "🕯️", "🌿", "💧", "🪷", "✨", "🌸"],
        ["entrenador-fuerza"] = ["🏋️", "💪", "🏃", "🔥", "⏱️", "🥗", "🏆", "⭐"],
        ["yoga-armonia"] = ["🧘", "🕉️", "🌿", "🪷", "☀️", "🌙", "✨", "💜"],
        ["cosmetologia-rostro"] = ["✨", "🧴", "🌸", "💆", "🪞", "💧", "🌹", "⭐"],
        ["taqueria-sabor"] = ["🌮", "🌶️", "🥑", "🍋", "🔥", "🧅", "🍺", "⭐"],
        ["panaderia-horno"] = ["🥖", "🍞", "🥐", "🧁", "🌾", "☕", "🔥", "⭐"],
        ["cafeteria-grano"] = ["☕", "🥐", "🍰", "🫘", "🧁", "🍪", "⭐", "🌿"],
        ["banquetes-fiesta"] = ["🍽️", "🍗", "🍛", "🥘", "🎉", "🥗", "🔥", "⭐"],
        ["inmobiliaria-llave"] = ["🏠", "🔑", "📍", "🏢", "🤝", "📋", "💼", "⭐"],
        ["marketing-digital"] = ["📣", "📱", "📈", "💡", "✨", "🎯", "❤️", "⭐"],
        ["floreria-petalos"] = ["🌹", "💐", "🌷", "🌻", "🌸", "🎁", "🌿", "⭐"],
        ["boutique-moda"] = ["👗", "👠", "👜", "🛍️", "✨", "💎", "🧣", "⭐"],
        ["mariachi-fiesta"] = ["🎺", "🎻", "🎸", "🪗", "🎶", "🇲🇽", "🌹", "⭐"],
        ["organizador-eventos"] = ["🎉", "🎊", "🎂", "💐", "🥂", "📅", "✨", "⭐"],
    };

    /// <summary>Emojis sugeridos de una plantilla por profesión (vacío si no es de este catálogo).</summary>
    public static string[] IconsFor(string code) => Icons.GetValueOrDefault(BaseCode(code)) ?? [];

    /// <summary>Quita el sufijo del diseño (-pildoras, -lineas, -burbujas, -perfil) para hallar la profesión base.</summary>
    public static string BaseCode(string code) =>
        Layouts.Select(l => "-" + l.Key).FirstOrDefault(sfx => code.EndsWith(sfx, StringComparison.Ordinal)) is { } sfx ? code[..^sfx.Length] : code;

    /// <summary>Cada servicio de la lista lleva el icono de la profesión (los cuatro primeros, en ciclo).</summary>
    private static string ServiceBullets(string code)
    {
        var icons = Icons.GetValueOrDefault(BaseCode(code)) ?? ["⭐"];
        var sb = new System.Text.StringBuilder();
        for (var n = 0; n < 4; n++)
            sb.Append($".svc li:nth-child(4n+{n + 1})::before{{content:\"{icons[n % icons.Length]}\"}}");
        return sb.ToString();
    }

    private static string Label(Tone tone) => tone switch
    {
        Tone.Formal => "Formal",
        Tone.Warm => "Cercano con iconos",
        _ => "Divertido con iconos",
    };

    public static IEnumerable<Template> All() =>
        Themes.Select(t => Build(t, null)).Concat(Themes.SelectMany(t => Layouts.Select(l => Build(t, l))));

    private static Template Build(Theme t, Layout? layout)
    {
        var html = layout is null ? Html.Replace("[[tone]]", t.Tone.ToString().ToLowerInvariant()) : layout.Html;
        var css = (layout is null ? BaseCss + ToneCss[t.Tone] : LayoutBaseCss + layout.Css) + ServiceBullets(t.Code);
        foreach (var (key, value) in new Dictionary<string, string>
        {
            ["bg"] = t.Bg, ["surface"] = t.Surface, ["text"] = t.Text, ["muted"] = t.Muted,
            ["primary"] = t.Primary, ["accent"] = t.Accent,
            
            ["head"] = $"'{t.HeadFont}',Georgia,serif", ["body"] = $"'{t.BodyFont}',system-ui,sans-serif",
        })
            css = css.Replace($"[[{key}]]", value);

        css = $"@import url('https://fonts.googleapis.com/css2?family={t.FontQuery}&display=swap');\n{css}";
        if (!t.HeadFont.Contains("Serif") && t.HeadFont is not ("Merriweather" or "Playfair Display" or "Lora"))
            css = css.Replace($"'{t.HeadFont}',Georgia,serif", $"'{t.HeadFont}',system-ui,sans-serif");

        return new Template
        {
            Name = layout is null ? t.Name : $"{t.Name} · {layout.Label}",
            Code = layout is null ? t.Code : $"{t.Code}-{layout.Key}",
            Description = layout is null ? $"{Label(t.Tone)} · {t.Description}" : $"{layout.Label} · {t.Description}",
            Category = t.Category,
            Profession = t.Profession,
            Html = html,
            Css = css,
            FieldKeys = string.Join(',', TarjetasPro.Application.Templates.TemplateRenderer.ExtractFields(html)),
        };
    }

    private const string Html = """
        <div class="page [[tone]]">
          <header>
            {{#foto}}<img class="avatar" src="{{foto}}" alt="{{nombre}}">{{/foto}}
            {{#logo}}<img class="logo" src="{{logo}}" alt="{{nombre}}">{{/logo}}
            <h1>{{nombre}}</h1>
            {{#puesto}}<p class="role">{{puesto}}</p>{{/puesto}}
            {{#empresa}}<p class="company">{{empresa}}</p>{{/empresa}}
            {{#eslogan}}<p class="slogan">{{eslogan}}</p>{{/eslogan}}
          </header>
          {{#descripcion}}<p class="bio">{{descripcion}}</p>{{/descripcion}}
          {{#whatsapp}}<a class="cta" href="https://wa.me/{{whatsapp}}">Escríbeme por WhatsApp</a>{{/whatsapp}}
          <div class="actions">
            {{#telefono}}<a class="btn" href="tel:{{telefono}}">📞 Llamar</a>{{/telefono}}
            {{#whatsapp2}}<a class="btn" href="https://wa.me/{{whatsapp2}}">WhatsApp 2</a>{{/whatsapp2}}
            {{#email}}<a class="btn" href="mailto:{{email}}">✉️ Correo</a>{{/email}}
            {{#web}}<a class="btn" href="{{web}}">🌐 Sitio web</a>{{/web}}
            {{#facebook}}<a class="btn" href="{{facebook}}">Facebook</a>{{/facebook}}
            {{#instagram}}<a class="btn" href="{{instagram}}">Instagram</a>{{/instagram}}
            {{#tiktok}}<a class="btn" href="{{tiktok}}">🎵 TikTok</a>{{/tiktok}}
            {{#linkedin}}<a class="btn" href="{{linkedin}}">💼 LinkedIn</a>{{/linkedin}}
          </div>
          {{#has_servicios}}<section class="svc"><h2>Servicios</h2><ul>{{#servicios}}<li>{{.}}</li>{{/servicios}}</ul></section>{{/has_servicios}}
          {{#ubicacion}}<section><h2>Ubicación</h2><p>📍 {{ubicacion}}</p>{{#mapa}}<p><a href="{{mapa}}">Ver en el mapa</a></p>{{/mapa}}</section>{{/ubicacion}}
          {{#horarios}}<section><h2>Horarios</h2><p>{{horarios}}</p></section>{{/horarios}}
        </div>
        """;

    private const string BaseCss = """
        *{box-sizing:border-box}body{margin:0;background:[[bg]];color:[[text]];font-family:[[body]]}
        .page{position:relative;max-width:440px;margin:0 auto;min-height:100vh;padding:28px 20px 48px}
        .page>*{position:relative;z-index:1}
        h1,h2{font-family:[[head]]}
        .avatar{width:112px;height:112px;object-fit:cover}.logo{max-width:180px;max-height:120px;object-fit:contain}
        h1{margin:12px 0 4px;font-size:1.9rem;line-height:1.15}
        .role{margin:0;color:[[primary]];font-weight:700}.company{margin:2px 0 0;color:[[muted]]}.slogan{margin:8px 0 0;font-style:italic;color:[[muted]]}
        .bio{margin:18px 0;line-height:1.55}
        .cta{display:block;text-align:center;padding:14px;margin:18px 0 10px;background:[[primary]];color:#fff;text-decoration:none;font-weight:700;font-size:1.05rem}
        .actions{display:grid;grid-template-columns:1fr 1fr;gap:10px;margin:10px 0 6px}
        .btn{display:block;text-align:center;padding:11px 8px;background:[[surface]];color:[[primary]];text-decoration:none;font-weight:600;font-size:.9rem;border:2px solid [[primary]]}
        section{margin:24px 0}h2{margin:0 0 8px;font-size:1rem;color:[[primary]]}
        section p{margin:0 0 6px;line-height:1.5}section a{color:[[primary]];font-weight:700}
        .svc ul{list-style:none;margin:0;padding:0;display:grid;gap:8px}
        .svc li{display:flex;align-items:center;gap:10px;padding:10px 12px;background:[[surface]];font-size:.92rem}
        .svc li::before{font-size:1.3rem;flex:none}
        section{background:color-mix(in srgb,[[surface]] 80%,transparent);border-radius:14px;padding:12px 14px}
        section h2{margin-top:0}
        """;


    private record Layout(string Key, string Label, string Html, string Css);

    /// <summary>Diseños reutilizables: cada profesión los combina con su paleta y tipografía (Pediatra · Píldoras, etc.).</summary>
    private static readonly Layout[] Layouts =
    [
        new("pildoras", "Píldoras", PillsHtml, PillsCss),
        new("lineas", "Líneas", LinesHtml, LinesCss),
        new("burbujas", "Burbujas", BubblesHtml, BubblesCss),
        new("perfil", "Perfil", ProfileHtml, ProfileCss),
    ];

    private const string Head = """
        <header>
            {{#foto}}<img class="avatar" src="{{foto}}" alt="{{nombre}}">{{/foto}}
            {{#logo}}<img class="logo" src="{{logo}}" alt="{{nombre}}">{{/logo}}
            <h1>{{nombre}}</h1>
            {{#puesto}}<p class="role">{{puesto}}</p>{{/puesto}}
            {{#empresa}}<p class="company">{{empresa}}</p>{{/empresa}}
            {{#eslogan}}<p class="slogan">{{eslogan}}</p>{{/eslogan}}
          </header>
          {{#descripcion}}<p class="bio">{{descripcion}}</p>{{/descripcion}}
        """;

    private const string Tail = """
        {{#ubicacion}}<section class="loc"><h2>Ubicación</h2><p>{{ubicacion}}</p>{{#mapa}}<p><a href="{{mapa}}">Ver en el mapa</a></p>{{/mapa}}</section>{{/ubicacion}}
          {{#has_servicios}}<section class="svc"><h2>Servicios</h2><ul>{{#servicios}}<li>{{.}}</li>{{/servicios}}</ul></section>{{/has_servicios}}
          {{#horarios}}<section><h2>Horarios</h2><p>{{horarios}}</p></section>{{/horarios}}
        """;

    private const string PillsHtml =
        "<div class=\"page pil\">\n  " + Head + "\n  <nav class=\"pills\">\n" +
        """
            {{#whatsapp}}<a class="pill" href="https://wa.me/{{whatsapp}}"><i class="pi"></i><span>WhatsApp</span></a>{{/whatsapp}}
            {{#whatsapp2}}<a class="pill" href="https://wa.me/{{whatsapp2}}"><i class="pi"></i><span>WhatsApp 2</span></a>{{/whatsapp2}}
            {{#telefono}}<a class="pill" href="tel:{{telefono}}"><i class="pi">📞</i><span>Llamar</span></a>{{/telefono}}
            {{#email}}<a class="pill" href="mailto:{{email}}"><i class="pi">✉️</i><span>Correo</span></a>{{/email}}
            {{#facebook}}<a class="pill" href="{{facebook}}"><i class="pi"></i><span>Facebook</span></a>{{/facebook}}
            {{#instagram}}<a class="pill" href="{{instagram}}"><i class="pi"></i><span>Instagram</span></a>{{/instagram}}
            {{#web}}<a class="pill" href="{{web}}"><i class="pi">🌐</i><span>Sitio web</span></a>{{/web}}
            {{#tiktok}}<a class="pill" href="{{tiktok}}"><i class="pi">🎵</i><span>TikTok</span></a>{{/tiktok}}
            {{#linkedin}}<a class="pill" href="{{linkedin}}"><i class="pi">💼</i><span>LinkedIn</span></a>{{/linkedin}}
          </nav>
          """ + Tail + "\n</div>";

    private const string LinesHtml =
        "<div class=\"page lin\">\n  " + Head + "\n  <nav class=\"rows\">\n" +
        """
            {{#whatsapp}}<a class="btn row" href="https://wa.me/{{whatsapp}}"><span>WhatsApp</span><b>→</b></a>{{/whatsapp}}
            {{#whatsapp2}}<a class="btn row" href="https://wa.me/{{whatsapp2}}"><span>WhatsApp 2</span><b>→</b></a>{{/whatsapp2}}
            {{#telefono}}<a class="btn row" href="tel:{{telefono}}"><em>📞</em><span>Llamar</span><b>→</b></a>{{/telefono}}
            {{#email}}<a class="btn row" href="mailto:{{email}}"><em>✉️</em><span>Correo</span><b>→</b></a>{{/email}}
            {{#facebook}}<a class="btn row" href="{{facebook}}"><span>Facebook</span><b>→</b></a>{{/facebook}}
            {{#instagram}}<a class="btn row" href="{{instagram}}"><span>Instagram</span><b>→</b></a>{{/instagram}}
            {{#web}}<a class="btn row" href="{{web}}"><em>🌐</em><span>Sitio web</span><b>→</b></a>{{/web}}
            {{#tiktok}}<a class="btn row" href="{{tiktok}}"><em>🎵</em><span>TikTok</span><b>→</b></a>{{/tiktok}}
            {{#linkedin}}<a class="btn row" href="{{linkedin}}"><em>💼</em><span>LinkedIn</span><b>→</b></a>{{/linkedin}}
          </nav>
          """ + Tail + "\n</div>";

    private const string BubblesHtml =
        "<div class=\"page bbl\">\n  " + Head + "\n  <nav class=\"bubbles\">\n" +
        """
            {{#whatsapp}}<a class="btn bubble" href="https://wa.me/{{whatsapp}}"><span>WhatsApp</span></a>{{/whatsapp}}
            {{#whatsapp2}}<a class="btn bubble" href="https://wa.me/{{whatsapp2}}"><span>WhatsApp 2</span></a>{{/whatsapp2}}
            {{#telefono}}<a class="btn bubble" href="tel:{{telefono}}"><em>📞</em><span>Llamar</span></a>{{/telefono}}
            {{#email}}<a class="btn bubble" href="mailto:{{email}}"><em>✉️</em><span>Correo</span></a>{{/email}}
            {{#facebook}}<a class="btn bubble" href="{{facebook}}"><span>Facebook</span></a>{{/facebook}}
            {{#instagram}}<a class="btn bubble" href="{{instagram}}"><span>Instagram</span></a>{{/instagram}}
            {{#web}}<a class="btn bubble" href="{{web}}"><em>🌐</em><span>Sitio web</span></a>{{/web}}
            {{#tiktok}}<a class="btn bubble" href="{{tiktok}}"><em>🎵</em><span>TikTok</span></a>{{/tiktok}}
            {{#linkedin}}<a class="btn bubble" href="{{linkedin}}"><em>💼</em><span>LinkedIn</span></a>{{/linkedin}}
          </nav>
          """ + Tail + "\n</div>";

    private const string ProfileHtml =
        """
        <div class="page prf">
          <header>
            <div class="hero">
              {{#foto}}<img class="avatar" src="{{foto}}" alt="{{nombre}}">{{/foto}}
              {{#logo}}<img class="logo" src="{{logo}}" alt="{{nombre}}">{{/logo}}
            </div>
            <h1>{{nombre}}</h1>
            {{#puesto}}<p class="role">{{puesto}}</p>{{/puesto}}
            {{#empresa}}<p class="company">{{empresa}}</p>{{/empresa}}
            {{#eslogan}}<p class="slogan">{{eslogan}}</p>{{/eslogan}}
          </header>
          <nav class="chips">
            {{#whatsapp}}<a class="btn chip" href="https://wa.me/{{whatsapp}}"><span>WhatsApp</span></a>{{/whatsapp}}
            {{#telefono}}<a class="btn chip" href="tel:{{telefono}}"><em>📞</em><span>Llamar</span></a>{{/telefono}}
            {{#email}}<a class="btn chip" href="mailto:{{email}}"><em>✉️</em><span>Correo</span></a>{{/email}}
            {{#instagram}}<a class="btn chip" href="{{instagram}}"><span>Instagram</span></a>{{/instagram}}
            {{#facebook}}<a class="btn chip" href="{{facebook}}"><span>Facebook</span></a>{{/facebook}}
            {{#web}}<a class="btn chip" href="{{web}}"><em>🌐</em><span>Sitio web</span></a>{{/web}}
            {{#whatsapp2}}<a class="btn chip" href="https://wa.me/{{whatsapp2}}"><span>WhatsApp 2</span></a>{{/whatsapp2}}
            {{#tiktok}}<a class="btn chip" href="{{tiktok}}"><em>🎵</em><span>TikTok</span></a>{{/tiktok}}
            {{#linkedin}}<a class="btn chip" href="{{linkedin}}"><em>💼</em><span>LinkedIn</span></a>{{/linkedin}}
          </nav>
          {{#descripcion}}<p class="bio">{{descripcion}}</p>{{/descripcion}}
          {{#has_servicios}}<section class="svc"><h2>Servicios</h2><ul>{{#servicios}}<li>{{.}}</li>{{/servicios}}</ul></section>{{/has_servicios}}
          {{#ubicacion}}<section class="loc"><h2>Ubicación</h2><p>{{ubicacion}}</p>{{#mapa}}<p><a href="{{mapa}}">Ver en el mapa</a></p>{{/mapa}}</section>{{/ubicacion}}
          {{#horarios}}<section><h2>Horarios</h2><p>{{horarios}}</p></section>{{/horarios}}
        </div>
        """;

    private const string ProfileCss = """
        .prf{padding-top:0}
        .prf .hero{margin:0 -20px;min-height:170px;background:linear-gradient(135deg,[[primary]],[[accent]]);border-radius:0 0 44px 44px;overflow:hidden;display:grid;place-items:center}
        .prf .hero .avatar{display:block;width:100%;height:320px;border-radius:0}
        .prf .hero .logo{margin:36px auto;padding:12px 16px;background:#fff;border-radius:16px;max-width:70%}
        .prf header{text-align:center}
        .prf h1{margin-top:18px;font-size:2.1rem}
        .prf .role{font-size:1.05rem}
        .prf .chips{display:grid;grid-template-columns:repeat(4,1fr);gap:10px;margin:20px 0 6px}
        .prf a.chip{display:flex;flex-direction:column;align-items:center;gap:6px;padding:10px 2px;border:0;border-radius:18px;background:[[surface]];color:[[primary]];text-decoration:none;font-weight:700;font-size:.7rem;text-align:center;box-shadow:0 4px 12px color-mix(in srgb,[[primary]] 18%,transparent)}
        .prf a.chip.btn::before{margin:0;width:1.7em;height:1.7em;font-size:1.1rem}
        .prf a.chip em{font-style:normal;font-size:1.5rem;line-height:1}
        .prf .bio{text-align:center;margin:20px 4px}
        .prf section{background:color-mix(in srgb,[[surface]] 85%,transparent);border-radius:22px;padding:14px 16px}
        .prf h2{text-transform:uppercase;letter-spacing:.14em;font-size:.78rem}
        .prf .svc ul{display:grid;grid-template-columns:1fr 1fr;gap:8px 12px}
        .prf .svc li{font-size:.82rem;line-height:1.25}
        """;

    private const string LayoutBaseCss = """
        *{box-sizing:border-box}body{margin:0;background:[[bg]];color:[[text]];font-family:[[body]]}
        .page{position:relative;max-width:440px;margin:0 auto;min-height:100vh;padding:28px 20px 56px;overflow:hidden}
        .page>*{position:relative;z-index:1}
        h1,h2{font-family:[[head]]}
        .avatar{width:112px;height:112px;object-fit:cover;border-radius:50%}.logo{max-width:200px;max-height:130px;object-fit:contain}
        h1{margin:12px 0 4px;font-size:1.9rem;line-height:1.15}
        .role{margin:0;color:[[primary]];font-weight:700}.company{margin:2px 0 0;color:[[muted]]}.slogan{margin:8px 0 0;font-style:italic;color:[[muted]]}
        .bio{margin:16px 0;line-height:1.55}
        section{margin:22px 0}h2{margin:0 0 10px;font-size:.9rem;color:[[primary]]}section p{margin:0 0 6px;line-height:1.5}section a{color:[[primary]];font-weight:700}
        .svc ul{list-style:none;margin:0;padding:0}.svc li{display:flex;align-items:center;gap:8px;font-size:.88rem}.svc li::before{font-size:1.25rem;flex:none}
        """;

    private const string PillsCss = """
        .pil header{text-align:center}
        .pil::before{content:"";position:absolute;inset:0;z-index:0;pointer-events:none;background:radial-gradient(circle at 100% 0,color-mix(in srgb,[[accent]] 40%,transparent) 0 110px,transparent 111px),radial-gradient(circle at 0 58%,color-mix(in srgb,[[primary]] 20%,transparent) 0 64px,transparent 65px),radial-gradient(circle at 100% 100%,color-mix(in srgb,[[accent]] 28%,transparent) 0 150px,transparent 151px)}
        .pills{display:grid;gap:12px;margin:22px 0}
        .pill{display:flex;align-items:center;gap:12px;padding:7px 14px 7px 7px;background:[[surface]];color:[[text]];text-decoration:none;font-weight:700;font-size:.86rem;letter-spacing:.07em;text-transform:uppercase;border-radius:999px;border:1px solid color-mix(in srgb,[[primary]] 28%,transparent);box-shadow:0 4px 0 color-mix(in srgb,[[primary]] 22%,transparent),0 8px 16px rgba(0,0,0,.07)}
        .pill .pi{flex:none;width:40px;height:40px;border-radius:50%;display:grid;place-items:center;background:[[primary]];color:#fff;font-style:normal;font-size:1.05rem}
        .pill:nth-child(even) .pi{background:[[accent]]}
        .pill span{flex:1;text-align:center;border-left:1px solid color-mix(in srgb,[[primary]] 22%,transparent);padding:2px 0 2px 10px}
        .pill::after{content:"";flex:none;width:9px;height:9px;border-top:3px solid [[primary]];border-right:3px solid [[primary]];transform:rotate(45deg);margin:0 8px 0 2px}
        .pil section{text-align:center}
        .pil h2{display:flex;align-items:center;gap:10px;text-transform:uppercase;letter-spacing:.16em;font-size:.82rem}
        .pil h2::before,.pil h2::after{content:"";flex:1;height:1px;background:[[accent]]}
        .pil .loc h2::before{content:none}.pil .loc h2{justify-content:center}.pil .loc h2::after{content:none}
        .pil .loc::before{content:"📍";display:block;font-size:1.3rem}
        .pil .svc ul{display:grid;grid-template-columns:1fr 1fr;gap:10px 14px;text-align:left}
        .pil .svc li{font-size:.8rem;line-height:1.25}
        """;

    private const string LinesCss = """
        .lin header{text-align:left;border-bottom:2px solid [[primary]];padding-bottom:18px}
        .lin .avatar{border-radius:8px}
        .rows{margin:18px 0}
        .lin a.row{display:flex;align-items:center;gap:12px;padding:15px 2px;border:0;border-bottom:1px solid color-mix(in srgb,[[text]] 16%,transparent);background:none;color:[[text]];text-decoration:none;font-weight:600;border-radius:0}
        .lin a.row.btn::before{flex:none;margin:0;color:[[primary]]}
        .lin a.row em{font-style:normal;flex:none;width:1.15em;text-align:center}
        .lin a.row span{flex:1}.lin a.row b{color:[[primary]];font-weight:700}
        .lin section{border-top:1px solid color-mix(in srgb,[[text]] 16%,transparent);padding-top:16px}
        .lin h2{text-transform:uppercase;letter-spacing:.2em;font-size:.72rem;color:[[muted]]}
        .lin .svc li{padding:9px 0;border-bottom:1px solid color-mix(in srgb,[[text]] 10%,transparent)}
        """;

    private const string BubblesCss = """
        .bbl header{text-align:center}
        .bbl::before{content:"";position:absolute;inset:0;z-index:0;pointer-events:none;background:radial-gradient(circle at 12% 6%,color-mix(in srgb,[[accent]] 35%,transparent) 0 46px,transparent 47px),radial-gradient(circle at 88% 12%,color-mix(in srgb,[[primary]] 28%,transparent) 0 70px,transparent 71px),radial-gradient(circle at 80% 46%,color-mix(in srgb,[[accent]] 22%,transparent) 0 26px,transparent 27px),radial-gradient(circle at 6% 64%,color-mix(in srgb,[[primary]] 22%,transparent) 0 54px,transparent 55px),radial-gradient(circle at 92% 90%,color-mix(in srgb,[[accent]] 32%,transparent) 0 90px,transparent 91px),radial-gradient(circle at 20% 98%,color-mix(in srgb,[[primary]] 20%,transparent) 0 40px,transparent 41px)}
        .bbl .avatar{border:5px solid [[accent]];box-shadow:0 0 0 9px color-mix(in srgb,[[accent]] 25%,transparent)}
        .bubbles{display:grid;grid-template-columns:repeat(3,1fr);gap:12px;margin:22px 0}
        .bbl a.bubble{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:6px;min-height:92px;padding:12px 6px;border:0;border-radius:28px;background:[[surface]];color:[[text]];text-decoration:none;font-weight:700;font-size:.78rem;text-align:center;box-shadow:0 6px 16px color-mix(in srgb,[[primary]] 22%,transparent)}
        .bbl a.bubble.btn::before{margin:0;width:1.9em;height:1.9em;font-size:1.1rem;color:[[primary]]}
        .bbl a.bubble em{font-style:normal;font-size:1.7rem;line-height:1}
        .bbl section{text-align:center;background:color-mix(in srgb,[[surface]] 85%,transparent);border-radius:26px;padding:16px}
        .bbl h2{text-transform:uppercase;letter-spacing:.14em;font-size:.8rem}
        .bbl .svc ul{display:flex;flex-wrap:wrap;gap:8px;justify-content:center}
        .bbl .svc li{padding:7px 12px;border-radius:999px;background:[[bg]];border:1px solid color-mix(in srgb,[[primary]] 25%,transparent);font-size:.8rem}
        """;

    private static readonly Dictionary<Tone, string> ToneCss = new()
    {
        [Tone.Formal] = """
            .formal{background:[[surface]];border-top:6px solid [[primary]];box-shadow:0 0 0 1px rgba(0,0,0,.05)}
            .formal header{text-align:left;border-bottom:2px solid [[accent]];padding-bottom:16px}
            .formal .avatar{border-radius:6px;border:3px solid [[primary]]}
            .formal h1{font-size:1.7rem;letter-spacing:.01em}
            .formal .role{text-transform:uppercase;letter-spacing:.12em;font-size:.8rem}
            .formal .cta,.formal .btn,.formal .svc li{border-radius:4px}
            .formal h2{text-transform:uppercase;letter-spacing:.14em;font-size:.82rem;border-bottom:1px solid [[accent]];padding-bottom:6px}
            .formal .svc li{border-left:4px solid [[accent]];background:[[bg]]}
            """,
        [Tone.Warm] = """
            .warm header{text-align:center}
            .warm .page,.page.warm{background:linear-gradient(180deg,[[surface]] 0%,[[bg]] 100%)}
            .warm .avatar{border-radius:50%;border:4px solid [[accent]]}
            .warm .cta{border-radius:16px;box-shadow:0 6px 16px rgba(0,0,0,.14)}
            .warm .btn{border-radius:14px;border-color:transparent;box-shadow:0 3px 10px rgba(0,0,0,.08)}
            .warm h2{text-align:center}
            .warm section{text-align:center}
            .warm .svc li{border-radius:12px;text-align:left;box-shadow:0 2px 6px rgba(0,0,0,.06)}
            """,
        [Tone.Festive] = """
            .festive header{text-align:center;background:[[surface]];border-radius:28px;padding:22px 16px;border:4px solid [[primary]];box-shadow:6px 6px 0 [[accent]]}
            .festive .avatar{border-radius:50%;border:4px solid [[accent]]}
            .festive h1{font-size:2.2rem;color:[[text]]}
            .festive .role{font-size:1.1rem;text-transform:uppercase;letter-spacing:.06em}
            .festive .cta{border-radius:999px;font-size:1.2rem;box-shadow:4px 4px 0 [[accent]]}
            .festive .btn{border-radius:999px;border-width:3px;font-weight:800}
            .festive h2{display:inline-block;background:[[accent]];color:#fff;padding:4px 14px;border-radius:999px;mix-blend-mode:normal}
            .festive section{text-align:center;background:[[surface]];border-radius:20px;padding:16px;margin:20px 0}
            .festive .svc li{border-radius:999px;border:2px dashed [[primary]]}
            """,
    };
}
