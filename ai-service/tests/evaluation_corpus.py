"""
Comprehensive 300+ item Evaluation Corpus for SurplusLink AI Assistant.
Covers English, Sinhala Unicode, Romanized Sinhala (si-Latn), Tamil, mixed languages,
unit formats, noisy Sri Lankan terms, package counts, dimensions, and topic switches.
"""

PREFIXES = [
    "",
    "please ",
    "quick one: ",
    "machan, ",
    "මට කියන්න: ",
    "bro ",
    "ai assistant, ",
    "can you tell me ",
]

BASES = {
    "CONSTRUCTION_KNOWLEDGE": [
        "is dampness harmful when cement sacks are kept for a while",
        "leftover wall coating tika safe widiyata tiyaganne kohomada",
        "වානේ කම්බි මලකඩෙන් බේරාගන්නේ කොහොමද",
        "மீதமுள்ள டைல்களை எப்படி பாதுகாப்பாக வைப்பது",
        "PVC pipe tika eliye thibbama monawada wenne",
        "tiles break wenne nathi widiyata transport karanne kohomada",
        "paint can store karanne kohomada",
        "how to store cement bags safely at site",
        "what is the recommended curing time for C25 concrete",
        "weather-shield paint apply karanna kalin primer gahannama ooneda",
    ],
    "MATERIAL_ESTIMATION_QUESTION": [
        "work out the coating needed for a 24 square metre wall twice over",
        "room ekak coats dekak gahanna paint litres keeyak yai da",
        "වර්ග මීටර් 30කට තීන්ත ප්‍රමාණය ගණන් කරන්න",
        "20 சதுர மீட்டருக்கு இரண்டு பூச்சு என்றால் எவ்வளவு பெயிண்ட்",
        "five hundred kilos equals how many standard cement sacks",
        "10m2 area ekakata paint kochchara ooneda",
        "room eka 12ft x 10ft height 9ft. paint kochchara yaida?",
        "600x600 tiles 15m2 ekakata box keeyak ooneda",
        "concrete cube ekaka cement bags keeyak yaida",
        "plastering 50m2 area ekakata cement keeyada",
    ],
    "PRICE_INFORMATION": [
        "what is the going rate for a medium can of masonry paint",
        "cement sack ekaka ganan kohomada dan",
        "වැලි කියුබ් එකක සාමාන්‍ය මිල දැනගන්න පුළුවන්ද",
        "ஒரு பெட்டி தரை ஓடுகளின் விலை என்ன",
        "roughly what budget should I expect for twelve litres",
        "paint 10L price keeyada normally?",
        "what is the average market price for 50kg cement bag in Sri Lanka",
        "steel rebar 12mm per kg price keeyada",
        "sand 1 cube market rate going price",
        "brick 1000 count average cost keeyada",
    ],
    "LIVE_MARKETPLACE_QUERY": [
        "site eke sellers lage paint price kohomada",
        "show active cement listings near Malabe",
        "danata sellers lage thiyena items monada",
        "are there any sellers offering 600x600 tiles in Colombo",
        "browse current marketplace listings for steel bars",
        "available paint cans in Nugegoda",
        "market eke thiyena lowest price cement",
        "show me available PVC pipe sellers",
        "sellers with 50kg cement bags right now",
        "check active marketplace inventory for bricks",
    ],
    "CREATE_REQUIREMENT_DRAFT": [
        "source seven 50 kilogram cement sacks for our job in Homagama",
        "site ekata bricks desiyak aran denna Nugegoda langin",
        "මොරටුවට වැලි ඝන මීටර් දෙකහමාරක් සොයනවා",
        "கண்டிக்கு 20 PVC குழாய்கள் வாங்க வேண்டும்",
        "could you arrange about six litres of matt yellow wall coating near Malabe",
        "tomorrow site ekata cement ganna puluwanda",
        "i need 10L emulsion paint in Kandy",
        "cement bags 10k one malabe",
        "looking for 500 blocks near Maharagama",
        "want to buy 50 boxes of 600x600 floor tiles",
    ],
    "CONTINUE_REQUIREMENT_DRAFT": [
        "put the delivery point down as the junction near SLIIT",
        "kalin draft eke quantity eka eight karanna",
        "මම කලින් කිව්ව එකට සුදු පාට දාන්න",
        "முந்தைய கோரிக்கைக்கு இடம் யாழ்ப்பாணம்",
        "make that seven bags, each one fifty kilos",
        "50kg per bag",
        "make the colour Off-White",
        "delivery location is Colombo 03",
        "change package size to 20L cans",
        "add note: need delivery by tomorrow morning",
    ],
    "PLATFORM_HELP": [
        "walk me through posting unused building supplies",
        "seller kenek widiyata listing ekak danne kohomada",
        "ගැලපීමක් ලැබුණාට පස්සේ මොකද වෙන්නේ",
        "SurplusLink இல் வாங்குபவர் கோரிக்கை செய்வது எப்படி",
        "where do I confirm that the handover happened",
        "how does buyer/seller matching work in SurplusLink",
        "what are the payment approval rules",
        "how to cancel a requirement draft",
        "what happens when seller accepts my offer",
        "how to leave seller feedback on SurplusLink",
    ],
    "LIVE_DATA_QUERY": [
        "show the current state of the offers on my account",
        "mage active listings monawada balanna",
        "මගේ ගනුදෙනු වල අලුත්ම තත්ත්වය පෙන්වන්න",
        "எனது திறந்த கோரிக்கைகளை காட்டு",
        "did any of my marketplace offers get accepted",
        "show my active requirements",
        "mage past transactions list eka pennanna",
        "check my pending matches",
        "my current listings overview",
        "my saved offers status",
    ],
    "MATCH_EXPLANATION": [
        "explain why the first recommendation outranked the others",
        "mage match score eka wadi une mokak nisa da",
        "මේ විකුණුම්කරු හොඳ ගැලපීමක් වුණේ ඇයි",
        "இந்த பொருத்தத்திற்கு அதிக மதிப்பெண் ஏன்",
        "which factors pushed that listing to the top",
        "why did this listing get 95% match score",
        "explain match breakdown for recommendation #1",
        "why is seller A preferred over seller B",
        "what parameters determined this match ranking",
        "how was logistics distance calculated for this recommendation",
    ],
}

# Generate rich variations across prefixes to reach 300+ items
CORPUS = {}
total_count = 0
for intent, bases in BASES.items():
    items = [f"{prefix}{base}".strip() for prefix in PREFIXES for base in bases]
    CORPUS[intent] = items
    total_count += len(items)

NEGATIVE_REQUIREMENT_CASES = [
    "define cement in simple terms",
    "is a cement bag expensive this week",
    "tell me the correct way to keep cement dry",
    "convert half a tonne into cement sacks",
    "why is that cement recommendation ranked first",
    "does my listing inventory contain any cement",
    "paint 10L",
    "cement 5",
    "danata sellers lage thiyena items monada",
    "how to transport tiles safely",
]
