using System.Collections.Generic;
using Newtonsoft.Json;

public partial class Card
{
    public class CardIdentifiers
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("id")]
        public string? ID { get; set; }

        [JsonProperty("oracle_id")]
        public string? OracleID { get; set; }

        [JsonProperty("multiverse_ids")]
        public List<int>? MultiverseIDs { get; set; }

        [JsonProperty("mtgo_id")]
        public int? MTGOID { get; set; }

        [JsonProperty("mtgo_foil_id")]
        public int? MTGOFoilID { get; set; }

        [JsonProperty("tcgplayer_id")]
        public int? TCGPlayerID { get; set; }

        [JsonProperty("cardmarket_id")]
        public int? CardMarketID { get; set; }

        [JsonProperty("lang")]
        public string? Lang { get; set; }

        [JsonProperty("released_at")]
        public string? ReleasedAt { get; set; }

        [JsonProperty("uri")]
        public string? URI { get; set; }

        [JsonProperty("scryfall_uri")]
        public string? ScryfallUri { get; set; }

        [JsonProperty("layout")]
        public string? Layout { get; set; }

        [JsonProperty("highres_image")]
        public bool? HighResImage { get; set; }

        [JsonProperty("image_status")]
        public string? ImageStatus { get; set; }

        [JsonProperty("image_uris")]
        public ImageUris? ImageUris { get; set; }

        [JsonProperty("mana_cost")]
        public string? ManaCost { get; set; }

        [JsonProperty("cmc")]
        public float? CMC { get; set; }

        [JsonProperty("type_line")]
        public string? TypeLine { get; set; }

        [JsonProperty("oracle_text")]
        public string? OracleText { get; set; }

        [JsonProperty("power")]
        public string? Power { get; set; }

        [JsonProperty("toughness")]
        public string? Toughness { get; set; }

        [JsonProperty("colors")]
        public List<string> Colors { get; set; } = new List<string>();

        [JsonProperty("color_identity")]
        public List<string> ColorIdentity { get; set; } = new List<string>();

        [JsonProperty("keywords")]
        public List<string> Keywords { get; set; } = new List<string>();

        [JsonProperty("legalities")]
        public Legalities? Legalities { get; set; }

        [JsonProperty("reserved")]
        public bool? Reserved { get; set; }

        [JsonProperty("foil")]
        public bool? Foil { get; set; }

        [JsonProperty("nonfoil")]
        public bool? NonFoil { get; set; }

        [JsonProperty("finishes")]
        public List<string> Finishes { get; set; } = new List<string>();

        [JsonProperty("promo")]
        public bool? Promo { get; set; }

        [JsonProperty("reprint")]
        public bool? Reprint { get; set; }

        [JsonProperty("variation")]
        public bool? Variation { get; set; }

        [JsonProperty("set_id")]
        public string? SetID { get; set; }

        [JsonProperty("set")]
        public string? Set { get; set; }

        [JsonProperty("set_name")]
        public string? SetName { get; set; }

        [JsonProperty("set_type")]
        public string? SetType { get; set; }

        [JsonProperty("set_uri")]
        public string? SetUri { get; set; }

        [JsonProperty("set_search_uri")]
        public string? SetSearchUri { get; set; }

        [JsonProperty("rulings_uri")]
        public string? RulingsUri { get; set; }

        [JsonProperty("prints_search_uri")]
        public string? PrintsSearchUri { get; set; }

        [JsonProperty("collector_number")]
        public string? CollectorNumber { get; set; }

        [JsonProperty("digital")]
        public bool? Digital { get; set; }

        [JsonProperty("rarity")]
        public string? Rarity { get; set; }

        [JsonProperty("card_back_id")]
        public string? CardBackId { get; set; }

        [JsonProperty("artist")]
        public string? Artist { get; set; }

        [JsonProperty("artist_ids")]
        public List<string>? ArtistIDs { get; set; }

        [JsonProperty("illustration_id")]
        public string? IllustrationID { get; set; }

        [JsonProperty("border_color")]
        public string? BorderColor { get; set; }

        [JsonProperty("frame")]
        public string? Frame { get; set; }

        [JsonProperty("full_art")]
        public bool? FullArt { get; set; }

        [JsonProperty("textless")]
        public bool? Textless { get; set; }

        [JsonProperty("booster")]
        public bool? Booster { get; set; }

        [JsonProperty("story_spotlight")]
        public bool? StorySpotlight { get; set; }

        [JsonProperty("edhrec_rank")]
        public int? EDHRecRank { get; set; }

        [JsonProperty("penny_rank")]
        public int? PennyRank { get; set; }

        [JsonProperty("card_faces")]
        public List<CardFace>? CardFaces { get; set; }

        [JsonProperty("prices")]
        public Prices? Prices { get; set; }

        [JsonProperty("related_uris")]
        public RelatedUris? RelatedUris { get; set; }

        [JsonProperty("purchase_uris")]
        public PurchaseUris? PurchaseUris { get; set; }
    }

    public class CardFace
    {
        [JsonProperty("image_uris")]
        public ImageUris? ImageUris { get; set; }
    }

    public class ImageUris
    {
        [JsonProperty("small")]
        public string? Small { get; set; }

        [JsonProperty("normal")]
        public string? Normal { get; set; }

        [JsonProperty("large")]
        public string? Large { get; set; }

        [JsonProperty("png")]
        public string? Png { get; set; }

        [JsonProperty("art_crop")]
        public string? ArtCrop { get; set; }

        [JsonProperty("border_crop")]
        public string? BorderCrop { get; set; }
    }

    public class Legalities
    {
        [JsonProperty("standard")]
        public string? Standard { get; set; }

        [JsonProperty("pioneer")]
        public string? Pioneer { get; set; }

        [JsonProperty("modern")]
        public string? Modern { get; set; }

        [JsonProperty("legacy")]
        public string? Legacy { get; set; }

        [JsonProperty("vintage")]
        public string? Vintage { get; set; }

        [JsonProperty("commander")]
        public string? Commander { get; set; }

        [JsonProperty("oathbreaker")]
        public string? Oathbreaker { get; set; }

        [JsonProperty("alchemy")]
        public string? Alchemy { get; set; }

        [JsonProperty("explorer")]
        public string? Explorer { get; set; }

        [JsonProperty("historic")]
        public string? Historic { get; set; }

        [JsonProperty("brawl")]
        public string? Brawl { get; set; }

        [JsonProperty("timeless")]
        public string? Timeless { get; set; }

        [JsonProperty("pauper")]
        public string? Pauper { get; set; }

        [JsonProperty("penny")]
        public string? Penny { get; set; }

    }

    public class Prices
    {
        [JsonProperty("usd")]
        public string? USD { get; set; }

        [JsonProperty("usd_foil")]
        public string? USDFoil { get; set; }

        [JsonProperty("eur")]
        public string? EUR { get; set; }

        [JsonProperty("eur_foil")]
        public string? EURFoil { get; set; }

        [JsonProperty("tix")]
        public string? Tix { get; set; }
    }

    public class RelatedUris
    {
        [JsonProperty("gatherer")]
        public string? Gatherer { get; set; }

        [JsonProperty("tcgplayer_infinite_articles")]
        public string? TCGPlayerInfiniteArticles { get; set; }

        [JsonProperty("tcgplayer_infinite_decks")]
        public string? TCGPlayerInfiniteDecks { get; set; }

        [JsonProperty("edhrec")]
        public string? EdhrecLink { get; set; }
    }

    public class PurchaseUris
    {
        [JsonProperty("tcgplayer")]
        public string? TCGPlayer { get; set; }

        [JsonProperty("cardmarket")]
        public string? cardmarket { get; set; }

        [JsonProperty("cardhoarder")]
        public string? cardhoarder { get; set; }
    }
}