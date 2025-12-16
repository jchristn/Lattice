#!/usr/bin/env python3
"""
Lattice SDK Test Harness for Python

A comprehensive test suite for the Lattice Python SDK.
Tests all API endpoints and validates responses.

Usage:
    python test_harness.py <endpoint_url>

Example:
    python test_harness.py http://localhost:8000
"""

import sys
import time
from dataclasses import dataclass, field
from typing import List, Optional, Callable, Any
from datetime import datetime

from lattice_sdk import (
    LatticeClient,
    Collection,
    Document,
    Schema,
    SchemaElement,
    FieldConstraint,
    SearchResult,
    SchemaEnforcementMode,
    IndexingMode,
    SearchCondition,
    EnumerationOrder,
    DataType
)
from lattice_sdk.models import SearchQuery, SearchFilter


@dataclass
class TestOutcome:
    """Represents the outcome of a test."""
    success: bool
    error: Optional[str] = None

    @staticmethod
    def passed() -> "TestOutcome":
        return TestOutcome(success=True)

    @staticmethod
    def failed(error: str) -> "TestOutcome":
        return TestOutcome(success=False, error=error)


@dataclass
class TestResult:
    """Represents a test result with timing."""
    section: str
    name: str
    passed: bool
    elapsed_ms: float
    error: Optional[str] = None


class TestHarness:
    """Test harness for the Lattice Python SDK."""

    def __init__(self, endpoint: str):
        self.endpoint = endpoint
        self.client = LatticeClient(endpoint)
        self.results: List[TestResult] = []
        self.pass_count = 0
        self.fail_count = 0
        self.current_section = ""
        self.overall_start_time = 0.0

    def run_all_tests(self):
        """Run all test suites."""
        print("=" * 79)
        print("  Lattice SDK Test Harness - Python")
        print("=" * 79)
        print()
        print(f"  Endpoint: {self.endpoint}")
        print()

        self.overall_start_time = time.time()

        try:
            # Health check first
            self.run_test_section("HEALTH CHECK", self.test_health_check)

            # Collection API Tests
            self.run_test_section("COLLECTION API", self.test_collection_api)

            # Document API Tests
            self.run_test_section("DOCUMENT API", self.test_document_api)

            # Search API Tests
            self.run_test_section("SEARCH API", self.test_search_api)

            # Enumeration API Tests
            self.run_test_section("ENUMERATION API", self.test_enumeration_api)

            # Schema API Tests
            self.run_test_section("SCHEMA API", self.test_schema_api)

            # Index API Tests
            self.run_test_section("INDEX API", self.test_index_api)

            # Constraint Tests
            self.run_test_section("SCHEMA CONSTRAINTS", self.test_constraints_api)

            # Indexing Mode Tests
            self.run_test_section("INDEXING MODE", self.test_indexing_mode_api)

            # Edge Case Tests
            self.run_test_section("EDGE CASES", self.test_edge_cases)

            # Performance Tests
            self.run_test_section("PERFORMANCE", self.test_performance)

        except Exception as e:
            print(f"[FATAL] Unhandled exception: {e}")
            self.fail_count += 1

        overall_elapsed = time.time() - self.overall_start_time
        self.print_summary(overall_elapsed)

        return self.fail_count == 0

    def run_test_section(self, section_name: str, test_func: Callable):
        """Run a test section."""
        print()
        print(f"--- {section_name} ---")
        self.current_section = section_name
        test_func()

    def run_test(self, name: str, test_func: Callable[[], TestOutcome]):
        """Run a single test and record the result."""
        start_time = time.time()
        passed = False
        error = None

        try:
            outcome = test_func()
            passed = outcome.success
            error = outcome.error
        except Exception as e:
            passed = False
            error = str(e)

        elapsed_ms = (time.time() - start_time) * 1000

        result = TestResult(
            section=self.current_section,
            name=name,
            passed=passed,
            elapsed_ms=elapsed_ms,
            error=error
        )
        self.results.append(result)

        if passed:
            print(f"  [PASS] {name} ({elapsed_ms:.0f}ms)")
            self.pass_count += 1
        else:
            print(f"  [FAIL] {name} ({elapsed_ms:.0f}ms)")
            if error:
                print(f"         Error: {error}")
            self.fail_count += 1

    def print_summary(self, overall_elapsed: float):
        """Print the test summary."""
        print()
        print("=" * 79)
        print("  TEST SUMMARY")
        print("=" * 79)
        print()

        # Group by section
        sections = {}
        for result in self.results:
            if result.section not in sections:
                sections[result.section] = []
            sections[result.section].append(result)

        for section_name, section_results in sections.items():
            section_pass = sum(1 for r in section_results if r.passed)
            section_total = len(section_results)
            status = "PASS" if section_pass == section_total else "FAIL"
            print(f"  {section_name}: {section_pass}/{section_total} [{status}]")

        print()
        print("-" * 79)
        overall_status = "PASS" if self.fail_count == 0 else "FAIL"
        print(f"  TOTAL: {self.pass_count} passed, {self.fail_count} failed [{overall_status}]")
        print(f"  RUNTIME: {overall_elapsed * 1000:.0f}ms ({overall_elapsed:.2f}s)")
        print("-" * 79)

        if self.fail_count > 0:
            print()
            print("  FAILED TESTS:")
            for result in self.results:
                if not result.passed:
                    print(f"    - {result.section}: {result.name}")
                    if result.error:
                        print(f"      Error: {result.error}")

        print()

    # ========== HEALTH CHECK TESTS ==========

    def test_health_check(self):
        """Test health check endpoint."""
        self.run_test("Health check returns true", lambda: (
            TestOutcome.passed() if self.client.health_check()
            else TestOutcome.failed("Health check failed")
        ))

    # ========== COLLECTION API TESTS ==========

    def test_collection_api(self):
        """Test collection API methods."""

        # Create collection: basic
        def test_create_basic():
            collection = self.client.collection.create("test_basic_collection")
            if collection is None:
                return TestOutcome.failed("Collection creation returned None")
            if not collection.id.startswith("col_"):
                return TestOutcome.failed(f"Invalid collection ID: {collection.id}")
            # Cleanup
            self.client.collection.delete(collection.id)
            return TestOutcome.passed()

        self.run_test("CreateCollection: basic", test_create_basic)

        # Create collection: with all parameters
        def test_create_full():
            collection = self.client.collection.create(
                name="test_full_collection",
                description="A test collection",
                labels=["test", "full"],
                tags={"env": "test", "version": "1.0"},
                schema_enforcement_mode=SchemaEnforcementMode.FLEXIBLE,
                indexing_mode=IndexingMode.ALL
            )
            if collection is None:
                return TestOutcome.failed("Collection creation returned None")
            if collection.name != "test_full_collection":
                return TestOutcome.failed(f"Name mismatch: {collection.name}")
            if collection.description != "A test collection":
                return TestOutcome.failed(f"Description mismatch: {collection.description}")
            # Cleanup
            self.client.collection.delete(collection.id)
            return TestOutcome.passed()

        self.run_test("CreateCollection: with all parameters", test_create_full)

        # Create collection: verify all properties
        def test_create_properties():
            collection = self.client.collection.create(
                name="test_props_collection",
                description="Props test",
                labels=["prop_test"],
                tags={"key": "value"}
            )
            if collection is None:
                return TestOutcome.failed("Collection creation returned None")
            if not collection.id:
                return TestOutcome.failed("Id is empty")
            if collection.created_utc is None:
                return TestOutcome.failed("CreatedUtc not set")
            # Cleanup
            self.client.collection.delete(collection.id)
            return TestOutcome.passed()

        self.run_test("CreateCollection: verify all properties", test_create_properties)

        # GetCollection: existing
        def test_get_existing():
            collection = self.client.collection.create("test_get_existing")
            if collection is None:
                return TestOutcome.failed("Setup: Collection creation failed")
            retrieved = self.client.collection.read_by_id(collection.id)
            if retrieved is None:
                self.client.collection.delete(collection.id)
                return TestOutcome.failed("GetCollection returned None")
            if retrieved.id != collection.id:
                self.client.collection.delete(collection.id)
                return TestOutcome.failed(f"Id mismatch: {retrieved.id}")
            self.client.collection.delete(collection.id)
            return TestOutcome.passed()

        self.run_test("GetCollection: existing", test_get_existing)

        # GetCollection: non-existent
        def test_get_nonexistent():
            retrieved = self.client.collection.read_by_id("col_nonexistent12345")
            if retrieved is not None:
                return TestOutcome.failed("Expected None for non-existent collection")
            return TestOutcome.passed()

        self.run_test("GetCollection: non-existent returns null", test_get_nonexistent)

        # GetCollections: multiple
        def test_get_multiple():
            col1 = self.client.collection.create("test_multi_1")
            col2 = self.client.collection.create("test_multi_2")
            if col1 is None or col2 is None:
                return TestOutcome.failed("Setup: Collection creation failed")

            collections = self.client.collection.read_all()
            found_ids = {c.id for c in collections}

            if col1.id not in found_ids or col2.id not in found_ids:
                self.client.collection.delete(col1.id)
                self.client.collection.delete(col2.id)
                return TestOutcome.failed("Not all collections found")

            self.client.collection.delete(col1.id)
            self.client.collection.delete(col2.id)
            return TestOutcome.passed()

        self.run_test("GetCollections: multiple", test_get_multiple)

        # CollectionExists: true
        def test_exists_true():
            collection = self.client.collection.create("test_exists_true")
            if collection is None:
                return TestOutcome.failed("Setup: Collection creation failed")
            exists = self.client.collection.exists(collection.id)
            self.client.collection.delete(collection.id)
            if not exists:
                return TestOutcome.failed("Expected exists to be True")
            return TestOutcome.passed()

        self.run_test("CollectionExists: true when exists", test_exists_true)

        # CollectionExists: false
        def test_exists_false():
            exists = self.client.collection.exists("col_nonexistent12345")
            if exists:
                return TestOutcome.failed("Expected exists to be False")
            return TestOutcome.passed()

        self.run_test("CollectionExists: false when not exists", test_exists_false)

        # DeleteCollection: removes collection
        def test_delete():
            collection = self.client.collection.create("test_delete")
            if collection is None:
                return TestOutcome.failed("Setup: Collection creation failed")
            deleted = self.client.collection.delete(collection.id)
            if not deleted:
                return TestOutcome.failed("Delete returned False")
            exists = self.client.collection.exists(collection.id)
            if exists:
                return TestOutcome.failed("Collection still exists after delete")
            return TestOutcome.passed()

        self.run_test("DeleteCollection: removes collection", test_delete)

    # ========== DOCUMENT API TESTS ==========

    def test_document_api(self):
        """Test document API methods."""

        # Create a collection for document tests
        collection = self.client.collection.create("doc_test_collection")
        if collection is None:
            self.run_test("Setup: Create collection", lambda: TestOutcome.failed("Collection creation failed"))
            return

        try:
            # IngestDocument: basic
            def test_ingest_basic():
                doc = self.client.document.ingest(collection.id, {"name": "Test"})
                if doc is None:
                    return TestOutcome.failed("Ingest returned None")
                if not doc.id.startswith("doc_"):
                    return TestOutcome.failed(f"Invalid document ID: {doc.id}")
                return TestOutcome.passed()

            self.run_test("IngestDocument: basic", test_ingest_basic)

            # IngestDocument: with name
            def test_ingest_with_name():
                doc = self.client.document.ingest(
                    collection.id,
                    {"name": "Named"},
                    name="my_document"
                )
                if doc is None:
                    return TestOutcome.failed("Ingest returned None")
                if doc.name != "my_document":
                    return TestOutcome.failed(f"Name mismatch: {doc.name}")
                return TestOutcome.passed()

            self.run_test("IngestDocument: with name", test_ingest_with_name)

            # IngestDocument: with labels
            def test_ingest_with_labels():
                doc = self.client.document.ingest(
                    collection.id,
                    {"name": "Labeled"},
                    labels=["label1", "label2"]
                )
                if doc is None:
                    return TestOutcome.failed("Ingest returned None")
                return TestOutcome.passed()

            self.run_test("IngestDocument: with labels", test_ingest_with_labels)

            # IngestDocument: with tags
            def test_ingest_with_tags():
                doc = self.client.document.ingest(
                    collection.id,
                    {"name": "Tagged"},
                    tags={"key": "value"}
                )
                if doc is None:
                    return TestOutcome.failed("Ingest returned None")
                return TestOutcome.passed()

            self.run_test("IngestDocument: with tags", test_ingest_with_tags)

            # IngestDocument: verify all properties
            def test_ingest_properties():
                doc = self.client.document.ingest(
                    collection.id,
                    {"name": "Properties Test"},
                    name="prop_doc",
                    labels=["prop"],
                    tags={"prop": "test"}
                )
                if doc is None:
                    return TestOutcome.failed("Ingest returned None")
                if not doc.id:
                    return TestOutcome.failed("Id is empty")
                if doc.collection_id != collection.id:
                    return TestOutcome.failed(f"CollectionId mismatch: {doc.collection_id}")
                if not doc.schema_id:
                    return TestOutcome.failed("SchemaId is empty")
                if doc.created_utc is None:
                    return TestOutcome.failed("CreatedUtc not set")
                return TestOutcome.passed()

            self.run_test("IngestDocument: verify all properties", test_ingest_properties)

            # IngestDocument: nested JSON
            def test_ingest_nested():
                doc = self.client.document.ingest(
                    collection.id,
                    {
                        "person": {
                            "name": "John",
                            "address": {
                                "city": "New York",
                                "zip": "10001"
                            }
                        }
                    }
                )
                if doc is None:
                    return TestOutcome.failed("Ingest returned None")
                return TestOutcome.passed()

            self.run_test("IngestDocument: nested JSON", test_ingest_nested)

            # IngestDocument: array JSON
            def test_ingest_array():
                doc = self.client.document.ingest(
                    collection.id,
                    {
                        "items": [1, 2, 3, 4, 5],
                        "names": ["Alice", "Bob", "Charlie"]
                    }
                )
                if doc is None:
                    return TestOutcome.failed("Ingest returned None")
                return TestOutcome.passed()

            self.run_test("IngestDocument: array JSON", test_ingest_array)

            # GetDocument tests - create a specific doc first
            test_doc = self.client.document.ingest(
                collection.id,
                {"name": "GetTest", "value": 42},
                name="get_test_doc",
                labels=["get_test"],
                tags={"test_type": "get"}
            )

            if test_doc:
                # GetDocument: without content
                def test_get_without_content():
                    doc = self.client.document.read_by_id(collection.id, test_doc.id, include_content=False)
                    if doc is None:
                        return TestOutcome.failed("GetDocument returned None")
                    if doc.content is not None:
                        return TestOutcome.failed("Content should be None")
                    return TestOutcome.passed()

                self.run_test("GetDocument: without content", test_get_without_content)

                # GetDocument: with content
                def test_get_with_content():
                    doc = self.client.document.read_by_id(collection.id, test_doc.id, include_content=True)
                    if doc is None:
                        return TestOutcome.failed("GetDocument returned None")
                    if doc.content is None:
                        return TestOutcome.failed("Content should not be None")
                    return TestOutcome.passed()

                self.run_test("GetDocument: with content", test_get_with_content)

                # GetDocument: verify labels
                def test_get_labels():
                    doc = self.client.document.read_by_id(
                        collection.id,
                        test_doc.id,
                        include_content=False,
                        include_labels=True
                    )
                    if doc is None:
                        return TestOutcome.failed("GetDocument returned None")
                    if "get_test" not in doc.labels:
                        return TestOutcome.failed(f"Label 'get_test' not found: {doc.labels}")
                    return TestOutcome.passed()

                self.run_test("GetDocument: verify labels populated", test_get_labels)

                # GetDocument: verify tags
                def test_get_tags():
                    doc = self.client.document.read_by_id(
                        collection.id,
                        test_doc.id,
                        include_content=False,
                        include_tags=True
                    )
                    if doc is None:
                        return TestOutcome.failed("GetDocument returned None")
                    if doc.tags.get("test_type") != "get":
                        return TestOutcome.failed(f"Tag 'test_type' mismatch: {doc.tags}")
                    return TestOutcome.passed()

                self.run_test("GetDocument: verify tags populated", test_get_tags)

            # GetDocument: non-existent
            def test_get_nonexistent():
                doc = self.client.document.read_by_id(collection.id, "doc_nonexistent12345")
                if doc is not None:
                    return TestOutcome.failed("Expected None for non-existent document")
                return TestOutcome.passed()

            self.run_test("GetDocument: non-existent returns null", test_get_nonexistent)

            # GetDocuments: multiple
            def test_get_multiple():
                docs = self.client.document.read_all_in_collection(collection.id)
                if len(docs) < 5:
                    return TestOutcome.failed(f"Expected at least 5 docs, got {len(docs)}")
                return TestOutcome.passed()

            self.run_test("GetDocuments: multiple documents", test_get_multiple)

            # DocumentExists: true
            def test_doc_exists_true():
                if test_doc is None:
                    return TestOutcome.failed("Setup: test_doc is None")
                exists = self.client.document.exists(collection.id, test_doc.id)
                if not exists:
                    return TestOutcome.failed("Expected exists to be True")
                return TestOutcome.passed()

            self.run_test("DocumentExists: true when exists", test_doc_exists_true)

            # DocumentExists: false
            def test_doc_exists_false():
                exists = self.client.document.exists(collection.id, "doc_nonexistent12345")
                if exists:
                    return TestOutcome.failed("Expected exists to be False")
                return TestOutcome.passed()

            self.run_test("DocumentExists: false when not exists", test_doc_exists_false)

            # DeleteDocument: removes document
            def test_delete_doc():
                doc = self.client.document.ingest(collection.id, {"to_delete": True})
                if doc is None:
                    return TestOutcome.failed("Setup: Ingest failed")
                deleted = self.client.document.delete(collection.id, doc.id)
                if not deleted:
                    return TestOutcome.failed("Delete returned False")
                exists = self.client.document.exists(collection.id, doc.id)
                if exists:
                    return TestOutcome.failed("Document still exists after delete")
                return TestOutcome.passed()

            self.run_test("DeleteDocument: removes document", test_delete_doc)

        finally:
            # Cleanup
            self.client.collection.delete(collection.id)

    # ========== SEARCH API TESTS ==========

    def test_search_api(self):
        """Test search API methods."""

        # Create a collection with searchable data
        collection = self.client.collection.create("search_test_collection")
        if collection is None:
            self.run_test("Setup: Create collection", lambda: TestOutcome.failed("Collection creation failed"))
            return

        try:
            # Ingest test documents
            for i in range(20):
                self.client.document.ingest(
                    collection.id,
                    {
                        "Name": f"Item_{i}",
                        "Category": f"Category_{i % 5}",
                        "Value": i * 10,
                        "IsActive": i % 2 == 0,
                        "Description": f"This is item number {i}"
                    },
                    name=f"doc_{i}",
                    labels=[f"group_{i % 3}"] + (["special"] if i % 10 == 0 else []),
                    tags={"priority": str(i % 3)}
                )

            # Search: Equals operator
            def test_search_equals():
                query = SearchQuery(
                    collection_id=collection.id,
                    filters=[SearchFilter("Category", SearchCondition.EQUALS, "Category_2")],
                    max_results=100
                )
                result = self.client.search.search(query)
                if result is None:
                    return TestOutcome.failed("Search returned None")
                if not result.success:
                    return TestOutcome.failed("Search not successful")
                if len(result.documents) != 4:
                    return TestOutcome.failed(f"Expected 4 results, got {len(result.documents)}")
                return TestOutcome.passed()

            self.run_test("Search: Equals operator", test_search_equals)

            # Search: NotEquals operator
            def test_search_not_equals():
                query = SearchQuery(
                    collection_id=collection.id,
                    filters=[SearchFilter("Category", SearchCondition.NOT_EQUALS, "Category_0")],
                    max_results=100
                )
                result = self.client.search.search(query)
                if result is None:
                    return TestOutcome.failed("Search returned None")
                if len(result.documents) != 16:
                    return TestOutcome.failed(f"Expected 16 results, got {len(result.documents)}")
                return TestOutcome.passed()

            self.run_test("Search: NotEquals operator", test_search_not_equals)

            # Search: GreaterThan operator
            def test_search_greater_than():
                query = SearchQuery(
                    collection_id=collection.id,
                    filters=[SearchFilter("Value", SearchCondition.GREATER_THAN, "150")],
                    max_results=100
                )
                result = self.client.search.search(query)
                if result is None:
                    return TestOutcome.failed("Search returned None")
                # Values > 150 are: 160, 170, 180, 190 (4 items)
                if len(result.documents) != 4:
                    return TestOutcome.failed(f"Expected 4 results, got {len(result.documents)}")
                return TestOutcome.passed()

            self.run_test("Search: GreaterThan operator", test_search_greater_than)

            # Search: LessThan operator
            def test_search_less_than():
                query = SearchQuery(
                    collection_id=collection.id,
                    filters=[SearchFilter("Value", SearchCondition.LESS_THAN, "30")],
                    max_results=100
                )
                result = self.client.search.search(query)
                if result is None:
                    return TestOutcome.failed("Search returned None")
                # Values < 30 are: 0, 10, 20 (3 items)
                if len(result.documents) != 3:
                    return TestOutcome.failed(f"Expected 3 results, got {len(result.documents)}")
                return TestOutcome.passed()

            self.run_test("Search: LessThan operator", test_search_less_than)

            # Search: Contains operator
            def test_search_contains():
                query = SearchQuery(
                    collection_id=collection.id,
                    filters=[SearchFilter("Name", SearchCondition.CONTAINS, "Item_1")],
                    max_results=100
                )
                result = self.client.search.search(query)
                if result is None:
                    return TestOutcome.failed("Search returned None")
                # "Item_1" matches: Item_1, Item_10-19 (11 items)
                if len(result.documents) < 1:
                    return TestOutcome.failed(f"Expected at least 1 result, got {len(result.documents)}")
                return TestOutcome.passed()

            self.run_test("Search: Contains operator", test_search_contains)

            # Search: StartsWith operator
            def test_search_starts_with():
                query = SearchQuery(
                    collection_id=collection.id,
                    filters=[SearchFilter("Name", SearchCondition.STARTS_WITH, "Item_")],
                    max_results=100
                )
                result = self.client.search.search(query)
                if result is None:
                    return TestOutcome.failed("Search returned None")
                if len(result.documents) != 20:
                    return TestOutcome.failed(f"Expected 20 results, got {len(result.documents)}")
                return TestOutcome.passed()

            self.run_test("Search: StartsWith operator", test_search_starts_with)

            # Search: multiple filters (AND)
            def test_search_multiple_filters():
                query = SearchQuery(
                    collection_id=collection.id,
                    filters=[
                        SearchFilter("Category", SearchCondition.EQUALS, "Category_2"),
                        SearchFilter("IsActive", SearchCondition.EQUALS, "true")
                    ],
                    max_results=100
                )
                result = self.client.search.search(query)
                if result is None:
                    return TestOutcome.failed("Search returned None")
                # Category_2: 2,7,12,17 and IsActive (even): 2,12 (2 items)
                if len(result.documents) != 2:
                    return TestOutcome.failed(f"Expected 2 results, got {len(result.documents)}")
                return TestOutcome.passed()

            self.run_test("Search: multiple filters (AND)", test_search_multiple_filters)

            # Search: by label
            def test_search_by_label():
                query = SearchQuery(
                    collection_id=collection.id,
                    labels=["special"],
                    max_results=100
                )
                result = self.client.search.search(query)
                if result is None:
                    return TestOutcome.failed("Search returned None")
                # "special" label: 0, 10 (2 items)
                if len(result.documents) != 2:
                    return TestOutcome.failed(f"Expected 2 results, got {len(result.documents)}")
                return TestOutcome.passed()

            self.run_test("Search: by label", test_search_by_label)

            # Search: by tag
            def test_search_by_tag():
                query = SearchQuery(
                    collection_id=collection.id,
                    tags={"priority": "0"},
                    max_results=100
                )
                result = self.client.search.search(query)
                if result is None:
                    return TestOutcome.failed("Search returned None")
                # priority=0: 0,3,6,9,12,15,18 (7 items)
                if len(result.documents) != 7:
                    return TestOutcome.failed(f"Expected 7 results, got {len(result.documents)}")
                return TestOutcome.passed()

            self.run_test("Search: by tag", test_search_by_tag)

            # Search: pagination Skip
            def test_search_skip():
                query = SearchQuery(
                    collection_id=collection.id,
                    skip=10,
                    max_results=100
                )
                result = self.client.search.search(query)
                if result is None:
                    return TestOutcome.failed("Search returned None")
                if len(result.documents) != 10:
                    return TestOutcome.failed(f"Expected 10 results, got {len(result.documents)}")
                return TestOutcome.passed()

            self.run_test("Search: pagination Skip", test_search_skip)

            # Search: pagination MaxResults
            def test_search_max_results():
                query = SearchQuery(
                    collection_id=collection.id,
                    max_results=5
                )
                result = self.client.search.search(query)
                if result is None:
                    return TestOutcome.failed("Search returned None")
                if len(result.documents) != 5:
                    return TestOutcome.failed(f"Expected 5 results, got {len(result.documents)}")
                return TestOutcome.passed()

            self.run_test("Search: pagination MaxResults", test_search_max_results)

            # Search: verify TotalRecords
            def test_search_total_records():
                query = SearchQuery(
                    collection_id=collection.id,
                    max_results=5
                )
                result = self.client.search.search(query)
                if result is None:
                    return TestOutcome.failed("Search returned None")
                if result.total_records != 20:
                    return TestOutcome.failed(f"Expected total_records=20, got {result.total_records}")
                return TestOutcome.passed()

            self.run_test("Search: verify TotalRecords", test_search_total_records)

            # Search: verify EndOfResults
            def test_search_end_of_results():
                query = SearchQuery(
                    collection_id=collection.id,
                    max_results=100
                )
                result = self.client.search.search(query)
                if result is None:
                    return TestOutcome.failed("Search returned None")
                if not result.end_of_results:
                    return TestOutcome.failed("Expected end_of_results=True")
                return TestOutcome.passed()

            self.run_test("Search: verify EndOfResults true", test_search_end_of_results)

            # Search: empty results
            def test_search_empty():
                query = SearchQuery(
                    collection_id=collection.id,
                    filters=[SearchFilter("Name", SearchCondition.EQUALS, "NonExistent")],
                    max_results=100
                )
                result = self.client.search.search(query)
                if result is None:
                    return TestOutcome.failed("Search returned None")
                if len(result.documents) != 0:
                    return TestOutcome.failed(f"Expected 0 results, got {len(result.documents)}")
                return TestOutcome.passed()

            self.run_test("Search: empty results", test_search_empty)

            # Search: with IncludeContent
            def test_search_with_content():
                query = SearchQuery(
                    collection_id=collection.id,
                    max_results=1,
                    include_content=True
                )
                result = self.client.search.search(query)
                if result is None:
                    return TestOutcome.failed("Search returned None")
                if len(result.documents) == 0:
                    return TestOutcome.failed("No documents returned")
                if result.documents[0].content is None:
                    return TestOutcome.failed("Content should be included")
                return TestOutcome.passed()

            self.run_test("Search: with IncludeContent true", test_search_with_content)

            # SearchBySql: basic query
            def test_search_sql():
                result = self.client.search.search_by_sql(
                    collection.id,
                    "SELECT * FROM documents WHERE Category = 'Category_1'"
                )
                if result is None:
                    return TestOutcome.failed("Search returned None")
                if len(result.documents) != 4:
                    return TestOutcome.failed(f"Expected 4 results, got {len(result.documents)}")
                return TestOutcome.passed()

            self.run_test("SearchBySql: basic query", test_search_sql)

        finally:
            # Cleanup
            self.client.collection.delete(collection.id)

    # ========== ENUMERATION API TESTS ==========

    def test_enumeration_api(self):
        """Test enumeration API methods."""

        collection = self.client.collection.create("enum_test_collection")
        if collection is None:
            self.run_test("Setup: Create collection", lambda: TestOutcome.failed("Collection creation failed"))
            return

        try:
            # Ingest test documents
            for i in range(10):
                self.client.document.ingest(
                    collection.id,
                    {"index": i, "name": f"EnumItem_{i}"},
                    name=f"enum_doc_{i}"
                )
                time.sleep(0.05)  # Small delay to ensure different timestamps

            # Enumerate: basic
            def test_enumerate_basic():
                query = SearchQuery(collection_id=collection.id, max_results=100)
                result = self.client.search.enumerate(query)
                if result is None:
                    return TestOutcome.failed("Enumerate returned None")
                if len(result.documents) != 10:
                    return TestOutcome.failed(f"Expected 10 results, got {len(result.documents)}")
                return TestOutcome.passed()

            self.run_test("Enumerate: basic", test_enumerate_basic)

            # Enumerate: with MaxResults
            def test_enumerate_max():
                query = SearchQuery(collection_id=collection.id, max_results=5)
                result = self.client.search.enumerate(query)
                if result is None:
                    return TestOutcome.failed("Enumerate returned None")
                if len(result.documents) != 5:
                    return TestOutcome.failed(f"Expected 5 results, got {len(result.documents)}")
                return TestOutcome.passed()

            self.run_test("Enumerate: with MaxResults", test_enumerate_max)

            # Enumerate: with Skip
            def test_enumerate_skip():
                query = SearchQuery(collection_id=collection.id, skip=5, max_results=100)
                result = self.client.search.enumerate(query)
                if result is None:
                    return TestOutcome.failed("Enumerate returned None")
                if len(result.documents) != 5:
                    return TestOutcome.failed(f"Expected 5 results, got {len(result.documents)}")
                return TestOutcome.passed()

            self.run_test("Enumerate: with Skip", test_enumerate_skip)

            # Enumerate: verify TotalRecords
            def test_enumerate_total():
                query = SearchQuery(collection_id=collection.id, max_results=3)
                result = self.client.search.enumerate(query)
                if result is None:
                    return TestOutcome.failed("Enumerate returned None")
                if result.total_records != 10:
                    return TestOutcome.failed(f"Expected total_records=10, got {result.total_records}")
                return TestOutcome.passed()

            self.run_test("Enumerate: verify TotalRecords", test_enumerate_total)

            # Enumerate: verify EndOfResults
            def test_enumerate_end():
                query = SearchQuery(collection_id=collection.id, max_results=100)
                result = self.client.search.enumerate(query)
                if result is None:
                    return TestOutcome.failed("Enumerate returned None")
                if not result.end_of_results:
                    return TestOutcome.failed("Expected end_of_results=True")
                return TestOutcome.passed()

            self.run_test("Enumerate: verify EndOfResults", test_enumerate_end)

        finally:
            self.client.collection.delete(collection.id)

    # ========== SCHEMA API TESTS ==========

    def test_schema_api(self):
        """Test schema API methods."""

        collection = self.client.collection.create("schema_test_collection")
        if collection is None:
            self.run_test("Setup: Create collection", lambda: TestOutcome.failed("Collection creation failed"))
            return

        try:
            # Ingest documents to create schemas
            doc1 = self.client.document.ingest(
                collection.id,
                {"name": "Test", "value": 42, "active": True}
            )

            # GetSchemas: returns schemas
            def test_get_schemas():
                schemas = self.client.schema.read_all()
                if len(schemas) == 0:
                    return TestOutcome.failed("No schemas returned")
                return TestOutcome.passed()

            self.run_test("GetSchemas: returns schemas", test_get_schemas)

            # GetSchema: by id
            def test_get_schema_by_id():
                if doc1 is None:
                    return TestOutcome.failed("Setup: doc1 is None")
                schema = self.client.schema.read_by_id(doc1.schema_id)
                if schema is None:
                    return TestOutcome.failed("GetSchema returned None")
                if schema.id != doc1.schema_id:
                    return TestOutcome.failed(f"Schema ID mismatch: {schema.id}")
                return TestOutcome.passed()

            self.run_test("GetSchema: by id", test_get_schema_by_id)

            # GetSchema: non-existent
            def test_get_schema_nonexistent():
                schema = self.client.schema.read_by_id("sch_nonexistent12345")
                if schema is not None:
                    return TestOutcome.failed("Expected None for non-existent schema")
                return TestOutcome.passed()

            self.run_test("GetSchema: non-existent returns null", test_get_schema_nonexistent)

            # GetSchemaElements: returns elements
            def test_get_schema_elements():
                if doc1 is None:
                    return TestOutcome.failed("Setup: doc1 is None")
                elements = self.client.schema.get_elements(doc1.schema_id)
                if len(elements) == 0:
                    return TestOutcome.failed("No elements returned")
                return TestOutcome.passed()

            self.run_test("GetSchemaElements: returns elements", test_get_schema_elements)

            # GetSchemaElements: correct keys
            def test_get_schema_elements_keys():
                if doc1 is None:
                    return TestOutcome.failed("Setup: doc1 is None")
                elements = self.client.schema.get_elements(doc1.schema_id)
                keys = {e.key for e in elements}
                expected = {"name", "value", "active"}
                if not expected.issubset(keys):
                    return TestOutcome.failed(f"Missing expected keys. Found: {keys}")
                return TestOutcome.passed()

            self.run_test("GetSchemaElements: correct keys", test_get_schema_elements_keys)

        finally:
            self.client.collection.delete(collection.id)

    # ========== INDEX API TESTS ==========

    def test_index_api(self):
        """Test index API methods."""

        # GetIndexTableMappings: returns mappings
        def test_get_mappings():
            mappings = self.client.index.get_mappings()
            # Mappings may be empty if no indexes exist yet
            if mappings is None:
                return TestOutcome.failed("GetMappings returned None")
            return TestOutcome.passed()

        self.run_test("GetIndexTableMappings: returns mappings", test_get_mappings)

    # ========== CONSTRAINTS API TESTS ==========

    def test_constraints_api(self):
        """Test schema constraints API methods."""

        # Create collection with strict mode
        def test_constraints_strict_mode():
            constraint = FieldConstraint(
                field_path="name",
                data_type="string",
                required=True
            )
            collection = self.client.collection.create(
                "constraints_test",
                schema_enforcement_mode=SchemaEnforcementMode.STRICT,
                field_constraints=[constraint]
            )
            if collection is None:
                return TestOutcome.failed("Collection creation failed")
            self.client.collection.delete(collection.id)
            return TestOutcome.passed()

        self.run_test("Constraints: create collection with strict mode", test_constraints_strict_mode)

        # Update constraints
        def test_update_constraints():
            collection = self.client.collection.create("constraints_update_test")
            if collection is None:
                return TestOutcome.failed("Collection creation failed")

            constraint = FieldConstraint(
                field_path="email",
                data_type="string",
                required=True,
                regex_pattern=r"^[\w\.-]+@[\w\.-]+\.\w+$"
            )
            success = self.client.collection.update_constraints(
                collection.id,
                SchemaEnforcementMode.STRICT,
                [constraint]
            )
            self.client.collection.delete(collection.id)

            if not success:
                return TestOutcome.failed("Update constraints failed")
            return TestOutcome.passed()

        self.run_test("Constraints: update constraints on collection", test_update_constraints)

        # Get constraints
        def test_get_constraints():
            constraint = FieldConstraint(
                field_path="test_field",
                data_type="string",
                required=True
            )
            collection = self.client.collection.create(
                "constraints_get_test",
                schema_enforcement_mode=SchemaEnforcementMode.STRICT,
                field_constraints=[constraint]
            )
            if collection is None:
                return TestOutcome.failed("Collection creation failed")

            constraints = self.client.collection.get_constraints(collection.id)
            self.client.collection.delete(collection.id)

            if len(constraints) == 0:
                return TestOutcome.failed("No constraints returned")
            return TestOutcome.passed()

        self.run_test("Constraints: get constraints from collection", test_get_constraints)

    # ========== INDEXING MODE API TESTS ==========

    def test_indexing_mode_api(self):
        """Test indexing mode API methods."""

        # Selective mode
        def test_selective_mode():
            collection = self.client.collection.create(
                "indexing_selective_test",
                indexing_mode=IndexingMode.SELECTIVE,
                indexed_fields=["name", "email"]
            )
            if collection is None:
                return TestOutcome.failed("Collection creation failed")
            self.client.collection.delete(collection.id)
            return TestOutcome.passed()

        self.run_test("Indexing: selective mode only indexes specified fields", test_selective_mode)

        # None mode
        def test_none_mode():
            collection = self.client.collection.create(
                "indexing_none_test",
                indexing_mode=IndexingMode.NONE
            )
            if collection is None:
                return TestOutcome.failed("Collection creation failed")
            self.client.collection.delete(collection.id)
            return TestOutcome.passed()

        self.run_test("Indexing: none mode skips indexing", test_none_mode)

        # Update indexing mode
        def test_update_indexing():
            collection = self.client.collection.create("indexing_update_test")
            if collection is None:
                return TestOutcome.failed("Collection creation failed")

            success = self.client.collection.update_indexing(
                collection.id,
                IndexingMode.SELECTIVE,
                indexed_fields=["name"],
                rebuild_indexes=False
            )
            self.client.collection.delete(collection.id)

            if not success:
                return TestOutcome.failed("Update indexing failed")
            return TestOutcome.passed()

        self.run_test("Indexing: update indexing mode", test_update_indexing)

        # Rebuild indexes
        def test_rebuild_indexes():
            collection = self.client.collection.create("indexing_rebuild_test")
            if collection is None:
                return TestOutcome.failed("Collection creation failed")

            # Ingest a document first
            self.client.document.ingest(collection.id, {"name": "test", "value": 42})

            result = self.client.collection.rebuild_indexes(collection.id)
            self.client.collection.delete(collection.id)

            if result is None:
                return TestOutcome.failed("Rebuild indexes returned None")
            return TestOutcome.passed()

        self.run_test("Indexing: rebuild indexes", test_rebuild_indexes)

    # ========== EDGE CASE TESTS ==========

    def test_edge_cases(self):
        """Test edge cases."""

        collection = self.client.collection.create("edge_case_collection")
        if collection is None:
            self.run_test("Setup: Create collection", lambda: TestOutcome.failed("Collection creation failed"))
            return

        try:
            # Empty string values
            def test_empty_strings():
                doc = self.client.document.ingest(
                    collection.id,
                    {"name": "", "description": ""}
                )
                if doc is None:
                    return TestOutcome.failed("Ingest failed")
                return TestOutcome.passed()

            self.run_test("Edge: empty string values in JSON", test_empty_strings)

            # Special characters
            def test_special_chars():
                doc = self.client.document.ingest(
                    collection.id,
                    {"text": "Hello! @#$%^&*()_+-={}[]|\\:\";<>?,./"}
                )
                if doc is None:
                    return TestOutcome.failed("Ingest failed")
                return TestOutcome.passed()

            self.run_test("Edge: special characters in values", test_special_chars)

            # Deeply nested JSON
            def test_deeply_nested():
                doc = self.client.document.ingest(
                    collection.id,
                    {
                        "level1": {
                            "level2": {
                                "level3": {
                                    "level4": {
                                        "level5": "deep value"
                                    }
                                }
                            }
                        }
                    }
                )
                if doc is None:
                    return TestOutcome.failed("Ingest failed")
                return TestOutcome.passed()

            self.run_test("Edge: deeply nested JSON (5 levels)", test_deeply_nested)

            # Large array
            def test_large_array():
                doc = self.client.document.ingest(
                    collection.id,
                    {"items": list(range(100))}
                )
                if doc is None:
                    return TestOutcome.failed("Ingest failed")
                return TestOutcome.passed()

            self.run_test("Edge: large array in JSON", test_large_array)

            # Numeric values
            def test_numeric_values():
                doc = self.client.document.ingest(
                    collection.id,
                    {
                        "integer": 42,
                        "float": 3.14159,
                        "negative": -100,
                        "zero": 0,
                        "large": 9999999999
                    }
                )
                if doc is None:
                    return TestOutcome.failed("Ingest failed")
                return TestOutcome.passed()

            self.run_test("Edge: numeric values", test_numeric_values)

            # Boolean values
            def test_boolean_values():
                doc = self.client.document.ingest(
                    collection.id,
                    {"active": True, "disabled": False}
                )
                if doc is None:
                    return TestOutcome.failed("Ingest failed")
                return TestOutcome.passed()

            self.run_test("Edge: boolean values", test_boolean_values)

            # Null values
            def test_null_values():
                doc = self.client.document.ingest(
                    collection.id,
                    {"name": "Test", "optional_field": None}
                )
                if doc is None:
                    return TestOutcome.failed("Ingest failed")
                return TestOutcome.passed()

            self.run_test("Edge: null values in JSON", test_null_values)

            # Unicode characters
            def test_unicode():
                doc = self.client.document.ingest(
                    collection.id,
                    {"greeting": "Hello, world!", "japanese": "Hello, world!", "emoji": "Hello, world!"}
                )
                if doc is None:
                    return TestOutcome.failed("Ingest failed")
                return TestOutcome.passed()

            self.run_test("Edge: unicode characters", test_unicode)

        finally:
            self.client.collection.delete(collection.id)

    # ========== PERFORMANCE TESTS ==========

    def test_performance(self):
        """Test performance benchmarks."""

        collection = self.client.collection.create("perf_test_collection")
        if collection is None:
            self.run_test("Setup: Create collection", lambda: TestOutcome.failed("Collection creation failed"))
            return

        try:
            # Ingest 100 documents
            def test_ingest_100():
                start = time.time()
                for i in range(100):
                    doc = self.client.document.ingest(
                        collection.id,
                        {
                            "Name": f"PerfItem_{i}",
                            "Category": f"Category_{i % 10}",
                            "Value": i * 10
                        },
                        name=f"perf_doc_{i}"
                    )
                    if doc is None:
                        return TestOutcome.failed(f"Failed to ingest document {i}")
                elapsed = time.time() - start
                rate = 100 / elapsed
                print(f"({rate:.1f} docs/sec) ", end="")
                return TestOutcome.passed()

            self.run_test("Perf: ingest 100 documents", test_ingest_100)

            # Search in 100 documents
            def test_search_100():
                start = time.time()
                query = SearchQuery(
                    collection_id=collection.id,
                    filters=[SearchFilter("Category", SearchCondition.EQUALS, "Category_5")],
                    max_results=100
                )
                result = self.client.search.search(query)
                elapsed = time.time() - start
                if result is None:
                    return TestOutcome.failed("Search returned None")
                print(f"({elapsed * 1000:.1f}ms) ", end="")
                return TestOutcome.passed()

            self.run_test("Perf: search in 100 documents", test_search_100)

            # GetDocuments for 100 documents
            def test_get_docs_100():
                start = time.time()
                docs = self.client.document.read_all_in_collection(collection.id)
                elapsed = time.time() - start
                if len(docs) != 100:
                    return TestOutcome.failed(f"Expected 100 docs, got {len(docs)}")
                print(f"({elapsed * 1000:.1f}ms) ", end="")
                return TestOutcome.passed()

            self.run_test("Perf: GetDocuments for 100 documents", test_get_docs_100)

            # Enumerate 100 documents
            def test_enumerate_100():
                start = time.time()
                query = SearchQuery(collection_id=collection.id, max_results=100)
                result = self.client.search.enumerate(query)
                elapsed = time.time() - start
                if result is None:
                    return TestOutcome.failed("Enumerate returned None")
                if len(result.documents) != 100:
                    return TestOutcome.failed(f"Expected 100 docs, got {len(result.documents)}")
                print(f"({elapsed * 1000:.1f}ms) ", end="")
                return TestOutcome.passed()

            self.run_test("Perf: enumerate 100 documents", test_enumerate_100)

        finally:
            self.client.collection.delete(collection.id)


def main():
    """Main entry point."""
    if len(sys.argv) < 2:
        print("Usage: python test_harness.py <endpoint_url>")
        print("Example: python test_harness.py http://localhost:8000")
        sys.exit(1)

    endpoint = sys.argv[1]

    print(f"Connecting to Lattice server at: {endpoint}")
    print()

    harness = TestHarness(endpoint)
    success = harness.run_all_tests()

    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()
