#!/bin/bash

# Test script for MCP Server
# This script sends JSON-RPC messages to the MCP server

set -e

MCP_SERVER_DIR="$(dirname "$0")/../src/MCPServer"

echo "====================================="
echo "Testing GraphRAG MCP Server"
echo "====================================="
echo ""

# Test 1: Initialize
echo "Test 1: Initialize"
echo '{"jsonrpc":"2.0","id":"1","method":"initialize","params":{}}' | \
  dotnet run --project "$MCP_SERVER_DIR" 2>/dev/null | \
  jq '.'
echo ""

# Test 2: List Tools
echo "Test 2: List Tools"
echo '{"jsonrpc":"2.0","id":"2","method":"tools/list","params":{}}' | \
  dotnet run --project "$MCP_SERVER_DIR" 2>/dev/null | \
  jq '.result.tools[] | {name: .name, description: .description}'
echo ""

# Test 3: Ping
echo "Test 3: Ping"
echo '{"jsonrpc":"2.0","id":"3","method":"ping","params":{}}' | \
  dotnet run --project "$MCP_SERVER_DIR" 2>/dev/null | \
  jq '.'
echo ""

echo "====================================="
echo "Basic MCP Server Tests Complete"
echo "====================================="
echo ""
echo "To test with actual Orchestrator API:"
echo "1. Start the Orchestrator API: cd src/OrchestratorAPI && dotnet run"
echo "2. Run: echo '{\"jsonrpc\":\"2.0\",\"id\":\"4\",\"method\":\"tools/call\",\"params\":{\"name\":\"graphrag_query\",\"arguments\":{\"query\":\"What is Service A?\"}}}' | dotnet run --project src/MCPServer"
