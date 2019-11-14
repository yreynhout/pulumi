// Copyright 2016-2018, Pulumi Corporation.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

import * as assert from "assert";
import { asyncTest } from "./util";


class Resource {
    /**
     * @internal
     * Private field containing the type ID for this object. Useful for implementing `isInstance` on
     * classes that inherit from `CustomResource`.
     */
    // tslint:disable-next-line:variable-name
    public readonly __pulumiType: string;

    static saveType(type: string) {
        (<any>this).__pulumiType = type;
    }

    public static isInstance(obj: any): obj is Resource {
        if (obj === undefined || obj === null) {
            return false;
        }
        return obj["__pulumiType"] === (<any>this).__pulumiType;
    }

    constructor(t: string) {
        this.__pulumiType = t;

        // Save the type as a "static" property on derived types.
        // (<any>this.constructor).__pulumiType = t;
        Resource.saveType(t);
    }

    public getType(): string {
        return this.__pulumiType;
    }
}

class MyResource extends Resource {
    constructor() {
        super("my");
    }
}

class FooResource extends Resource {
    constructor() {
        super("foo");
    }
}




describe("isInstance", () => {
    it("fails", asyncTest(async () => {
        //assert.ok(false);
    }));
});
