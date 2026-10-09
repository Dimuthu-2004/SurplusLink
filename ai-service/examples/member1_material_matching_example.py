import json
from app.agents.material_matching import MaterialMatchingAgent
from app.workflows.demo import demo_request
from app.workflows.orchestration import SnapshotTools

request = demo_request(); row = request.listings[0].model_copy(update={"availableQuantity": 5})
value = {"buyerUserId": request.buyerRequest["buyerId"], "categoryId": request.buyerRequest["categoryId"], "constructionItemTemplateId": request.buyerRequest["constructionItemTemplateId"], "requiredQuantity": 10, "unit": "kg", "maximumBudget": 2000, "deadline": request.buyerRequest["deadline"]}
result = MaterialMatchingAgent(SnapshotTools((row,))).match(value)
print("MEMBER 1 | Material Inventory & Listings | MaterialMatchingAgent")
print("INPUT:\n" + json.dumps(value, default=str, indent=2))
print("ALLOWED TOOLS: search_active_materials, get_material_detail")
print("OUTPUT:")
print(json.dumps(result.model_dump(mode="json"), indent=2))
print("Result: Seller can contribute 5 of the requested 10 kg; final allocation remains buyer-controlled.")
