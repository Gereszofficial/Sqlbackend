using SqlTrainer.Api.Dtos;

namespace SqlTrainer.Api.Services;

public static class RankBadge
{
    // Egyszerű, kiszámítható badge-ek százalék alapján.
    // Tone: UI-hoz egy "szín-hangulat" kulcs (frontend class mapping).
    public static RankBadgeDto ForPercent(int percent)
    {
        percent = Math.Clamp(percent, 0, 100);

        // Top rank: Gyémánt Legends (külön, látványos badge a frontendben)
        if (percent >= 95) return new RankBadgeDto("diamond_legends", "Gyémánt Legends", "legend");
        if (percent >= 80) return new RankBadgeDto("platinum", "Platina", "sky");
        if (percent >= 60) return new RankBadgeDto("gold", "Arany", "amber");
        if (percent >= 40) return new RankBadgeDto("silver", "Ezüst", "slate");
        if (percent >= 20) return new RankBadgeDto("bronze", "Bronz", "orange");
        return new RankBadgeDto("rookie", "Újonc", "emerald");
    }
}
