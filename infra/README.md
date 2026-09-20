# Infrastructure

Azure infrastructure is defined with Bicep in [azure](azure/README.md).

The initial template provisions the API hosting and the Azure dependencies documented by the architecture. It keeps service-specific model deployments and private-network topology out of the first deployment until those requirements are implemented.
