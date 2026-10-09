import json
from app.agents.requirement_planner import RequirementPlannerAgent
from app.workflows.demo import demo_request

request = demo_request()
value = {"buyerRequest": request.buyerRequest, "objective": "Find affordable cement near the delivery site."}
result = RequirementPlannerAgent().plan(value)
print("MEMBER 2 | Buyer Requirements | RequirementPlannerAgent")
print("INPUT:\n" + json.dumps(value, default=str, indent=2))
print("OUTPUT:")
print(json.dumps(result.model_dump(mode="json"), indent=2))
print("Result: validated requirement produces the fixed matching, logistics, validation, and manager-approval plan.")
